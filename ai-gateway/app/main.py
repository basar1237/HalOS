"""HalOS AI Gateway — FastAPI uygulaması (S2.4, ADR-002).

Uçlar:
- GET  /health           → basit sağlık kontrolü (docs/04 §8).
- POST /ai/ask            → AI muhasebeciye doğal dil sorusu (require_accountant).

/ai/ask akışı (SALT-OKUMA):
  1) JWT doğrula + tenant/role çıkar (require_accountant).
  2) ERP'den ilgili raporları çek (satış özeti + cari yaşlandırma) — kullanıcı token'ı
     ve tenant ile.
  3) build_accountant_prompt ile Claude/Stub promptunu kur.
  4) LlmClient.answer → cevap.
  5) {answer, used_sources, model} döndür.

LLM ve ERP istemcileri DI ile sağlanır; testler `dependency_overrides` ile stub enjekte eder.
"""

from __future__ import annotations

import logging
from datetime import date, datetime, timedelta, timezone
from typing import Any

from fastapi import Depends, FastAPI, HTTPException, status
from pydantic import BaseModel, Field, model_validator

from .auth import Principal, require_accountant
from .config import Settings, get_settings
from .erp_client import ErpReadClient, ErpUnavailableError, HttpErpReadClient
from .llm import LlmClient, StubLlmClient, build_llm_client
from .prompts import (
    build_accountant_prompt,
    build_document_extraction_prompt,
    build_insights_prompt,
    build_order_draft_prompt,
)

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("halos.ai.gateway")

app = FastAPI(
    title="HalOS AI Gateway",
    version="0.1.0",
    description="ERP ↔ AI güvenli sınırı (ADR-002). SALT-OKUMA; Claude tabanlı AI muhasebeci.",
)


# ---------------------------------------------------------------------------
# İstek / yanıt modelleri
# ---------------------------------------------------------------------------
class AskRequest(BaseModel):
    """AI muhasebeciye gönderilen soru ve opsiyonel tarih aralığı."""

    question: str = Field(..., min_length=1, description="Kullanıcının doğal dil sorusu.")
    date_from: date | None = Field(default=None, description="Satış özeti başlangıç tarihi (opsiyonel).")
    date_to: date | None = Field(default=None, description="Satış özeti bitiş tarihi (opsiyonel).")
    as_of: date | None = Field(default=None, description="Cari yaşlandırma referans tarihi (opsiyonel).")

    @model_validator(mode="before")
    @classmethod
    def _map_short_keys(cls, data: Any) -> Any:
        """API dostu kısa anahtarları (from/to/asOf) alan adlarına eşler.

        Not: Alan-bazlı alias yerine önden eşleme kullanılır; böylece FastAPI'nin
        gövde modelini alan-alan yeniden çözümlemesinden kaynaklanan pydantic
        `UnsupportedFieldAttributeWarning` uyarısı oluşmaz. Hem kısa (from/to/asOf)
        hem de tam (date_from/date_to/as_of) anahtarlar kabul edilir.
        """
        if isinstance(data, dict):
            mapping = {"from": "date_from", "to": "date_to", "asOf": "as_of"}
            for short, full in mapping.items():
                if short in data and full not in data:
                    data[full] = data[short]
        return data


class AskResponse(BaseModel):
    """AI muhasebeci yanıtı ve kullanılan veri kaynakları."""

    answer: str
    used_sources: list[str]
    model: str


class InsightsRequest(BaseModel):
    """Proaktif öneri isteği — opsiyonel tarih aralığı (yoksa son 30 gün / bugün)."""

    date_from: date | None = Field(default=None, description="Satış özeti başlangıcı (opsiyonel).")
    date_to: date | None = Field(default=None, description="Satış özeti bitişi (opsiyonel).")
    as_of: date | None = Field(default=None, description="Cari yaşlandırma referansı (opsiyonel).")

    @model_validator(mode="before")
    @classmethod
    def _map_short_keys(cls, data: Any) -> Any:
        if isinstance(data, dict):
            mapping = {"from": "date_from", "to": "date_to", "asOf": "as_of"}
            for short, full in mapping.items():
                if short in data and full not in data:
                    data[full] = data[short]
        return data


class InsightsResponse(BaseModel):
    """Proaktif AI önerileri (öncelikli aksiyon listesi, markdown metin)."""

    summary: str
    used_sources: list[str]
    model: str


class DraftOrderRequest(BaseModel):
    """Müşteri serbest metin mesajı (ör. WhatsApp) — taslak sipariş çıkarılır."""

    message: str = Field(..., min_length=1, description="Müşterinin sipariş mesajı.")


class DraftOrderResponse(BaseModel):
    """AI taslak sipariş (kullanıcı onayı gerektirir; sipariş OLUŞTURULMAZ)."""

    draft: str
    model: str
    disclaimer: str = "Bu bir taslaktır; kullanıcı onaylamadan sipariş oluşturulmaz."


class ReadDocumentRequest(BaseModel):
    """Belge metni (PDF/görselden çıkarılmış OCR metni) — taslak fatura/mal geliş çıkarılır."""

    document_text: str = Field(..., min_length=1, description="Belgenin metin içeriği (OCR sonucu).")
    doc_type: str | None = Field(default=None, description="Beklenen tür ipucu (invoice/consignment).")

    @model_validator(mode="before")
    @classmethod
    def _map_short_keys(cls, data: Any) -> Any:
        if isinstance(data, dict):
            mapping = {"documentText": "document_text", "docType": "doc_type"}
            for short, full in mapping.items():
                if short in data and full not in data:
                    data[full] = data[short]
        return data


class ReadDocumentResponse(BaseModel):
    """AI taslak belge (kullanıcı onayı gerektirir; kayıt OLUŞTURULMAZ)."""

    draft: str
    model: str
    disclaimer: str = "Bu bir taslaktır; kullanıcı onaylamadan kayıt oluşturulmaz."


class HealthResponse(BaseModel):
    status: str
    llm: str


# ---------------------------------------------------------------------------
# Bağımlılıklar (DI) — testlerde dependency_overrides ile değiştirilebilir
# ---------------------------------------------------------------------------
def get_llm_client(settings: Settings = Depends(get_settings)) -> LlmClient:
    """Yapılandırmaya göre Claude veya Stub LLM istemcisi sağlar."""
    return build_llm_client(settings)


def get_erp_client(settings: Settings = Depends(get_settings)) -> ErpReadClient:
    """httpx tabanlı SALT-OKUMA ERP istemcisi sağlar."""
    return HttpErpReadClient(
        sales_base_url=settings.erp.sales_base_url,
        finance_base_url=settings.erp.finance_base_url,
        timeout_seconds=settings.erp.request_timeout_seconds,
        service_token=settings.erp.service_token,
    )


def _model_name(llm: LlmClient) -> str:
    """LLM istemcisinin model adını en iyi çabayla döndürür."""
    return getattr(llm, "model", "unknown")


# ---------------------------------------------------------------------------
# Uçlar
# ---------------------------------------------------------------------------
@app.get("/health", response_model=HealthResponse, tags=["system"])
def health(
    settings: Settings = Depends(get_settings),
    llm: LlmClient = Depends(get_llm_client),
) -> HealthResponse:
    """Servis sağlık kontrolü + aktif LLM (claude/stub) bilgisi."""
    return HealthResponse(status="ok", llm=_model_name(llm))


@app.post("/ai/ask", response_model=AskResponse, tags=["ai"])
def ask(
    payload: AskRequest,
    principal: Principal = Depends(require_accountant),
    erp: ErpReadClient = Depends(get_erp_client),
    llm: LlmClient = Depends(get_llm_client),
) -> AskResponse:
    """AI muhasebeciye soru sorar. SALT-OKUMA; tenant kullanıcı token'ından çözülür."""
    # Makul varsayılan aralık: son 30 gün; as_of için bugün (UTC).
    today = datetime.now(timezone.utc).date()
    date_from = payload.date_from or (today - timedelta(days=30))
    date_to = payload.date_to or today
    as_of = payload.as_of or today

    erp_data: dict[str, object] = {}
    used_sources: list[str] = []

    try:
        erp_data["sales_summary"] = erp.get_sales_summary(
            principal.tenant_id, principal.token, date_from, date_to
        )
        used_sources.append("sales:/reports/sales-summary")

        erp_data["aging"] = erp.get_aging(principal.tenant_id, principal.token, as_of)
        used_sources.append("finance:/reports/aging")
    except ErpUnavailableError as exc:
        # ERP erişilemezse anlamlı hata (502): AI'a eksik veriyle cevap ürettirmeyiz.
        logger.warning("ERP okunamadı (tenant=%s): %s", principal.tenant_id, exc)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"ERP raporlarına erişilemedi: {exc}",
        ) from exc

    system_prompt, user_prompt = build_accountant_prompt(payload.question, erp_data)

    try:
        answer_text = llm.answer(system_prompt, user_prompt)
    except Exception as exc:  # noqa: BLE001 — LLM sağlayıcı hatalarını anlamlı 502'ye çevir
        logger.exception("LLM çağrısı başarısız (tenant=%s)", principal.tenant_id)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"AI modeline erişilemedi: {exc}",
        ) from exc

    return AskResponse(
        answer=answer_text,
        used_sources=used_sources,
        model=_model_name(llm),
    )


@app.post("/ai/insights", response_model=InsightsResponse, tags=["ai"])
def insights(
    payload: InsightsRequest,
    principal: Principal = Depends(require_accountant),
    erp: ErpReadClient = Depends(get_erp_client),
    llm: LlmClient = Depends(get_llm_client),
) -> InsightsResponse:
    """Proaktif AI ajanı (docs/06 S3.2): soru beklemeden ERP verisinden öncelikli
    uyarı/öneri üretir. SALT-OKUMA; tenant kullanıcı token'ından çözülür."""
    today = datetime.now(timezone.utc).date()
    date_from = payload.date_from or (today - timedelta(days=30))
    date_to = payload.date_to or today
    as_of = payload.as_of or today

    erp_data: dict[str, object] = {}
    used_sources: list[str] = []

    try:
        erp_data["sales_summary"] = erp.get_sales_summary(
            principal.tenant_id, principal.token, date_from, date_to
        )
        used_sources.append("sales:/reports/sales-summary")

        erp_data["aging"] = erp.get_aging(principal.tenant_id, principal.token, as_of)
        used_sources.append("finance:/reports/aging")
    except ErpUnavailableError as exc:
        logger.warning("ERP okunamadı (tenant=%s): %s", principal.tenant_id, exc)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"ERP raporlarına erişilemedi: {exc}",
        ) from exc

    system_prompt, user_prompt = build_insights_prompt(erp_data)

    try:
        summary_text = llm.answer(system_prompt, user_prompt)
    except Exception as exc:  # noqa: BLE001 — LLM sağlayıcı hatalarını anlamlı 502'ye çevir
        logger.exception("LLM çağrısı başarısız (tenant=%s)", principal.tenant_id)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"AI modeline erişilemedi: {exc}",
        ) from exc

    return InsightsResponse(
        summary=summary_text,
        used_sources=used_sources,
        model=_model_name(llm),
    )


@app.post("/ai/draft-order", response_model=DraftOrderResponse, tags=["ai"])
def draft_order(
    payload: DraftOrderRequest,
    principal: Principal = Depends(require_accountant),
    llm: LlmClient = Depends(get_llm_client),
) -> DraftOrderResponse:
    """Sipariş asistanı (docs/06 S3.3): müşteri mesajından TASLAK sipariş çıkarır.
    Kontrol kullanıcıdadır — sipariş OLUŞTURULMAZ, yalnız taslak döner. ERP'ye yazmaz."""
    system_prompt, user_prompt = build_order_draft_prompt(payload.message)

    try:
        draft_text = llm.answer(system_prompt, user_prompt)
    except Exception as exc:  # noqa: BLE001 — LLM sağlayıcı hatalarını anlamlı 502'ye çevir
        logger.exception("LLM çağrısı başarısız (tenant=%s)", principal.tenant_id)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"AI modeline erişilemedi: {exc}",
        ) from exc

    return DraftOrderResponse(draft=draft_text, model=_model_name(llm))


@app.post("/ai/read-document", response_model=ReadDocumentResponse, tags=["ai"])
def read_document(
    payload: ReadDocumentRequest,
    principal: Principal = Depends(require_accountant),
    llm: LlmClient = Depends(get_llm_client),
) -> ReadDocumentResponse:
    """Evrak okuma (docs/06 S3.6): belge metninden TASLAK fatura/mal geliş çıkarır.
    Kontrol kullanıcıdadır — kayıt OLUŞTURULMAZ, yalnız taslak döner. ERP'ye yazmaz."""
    system_prompt, user_prompt = build_document_extraction_prompt(
        payload.document_text, payload.doc_type
    )

    try:
        draft_text = llm.answer(system_prompt, user_prompt)
    except Exception as exc:  # noqa: BLE001 — LLM sağlayıcı hatalarını anlamlı 502'ye çevir
        logger.exception("LLM çağrısı başarısız (tenant=%s)", principal.tenant_id)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"AI modeline erişilemedi: {exc}",
        ) from exc

    return ReadDocumentResponse(draft=draft_text, model=_model_name(llm))

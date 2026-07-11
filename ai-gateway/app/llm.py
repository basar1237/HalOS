"""LLM istemcisi soyutlaması (Claude / Stub).

- `LlmClient` protokolü: tek bir `answer(system, user) -> str` metodu.
- `AnthropicLlmClient`: resmi `anthropic` SDK, Messages API üzerinden Claude çağırır.
- `StubLlmClient`: DIŞ ÇAĞRI YAPMAZ; girdiyi yansıtan deterministik Türkçe özet döner.
  Anahtarsız ortamda ve testlerde servisin çalışmasını sağlar.
- `build_llm_client(settings)`: anahtar varsa Anthropic, yoksa Stub döner (log'lar).

docs/04 ADR-002: birincil model Anthropic Claude. Anahtar yoksa gerçek çağrı YAPILMAZ.
"""

from __future__ import annotations

import logging
from typing import Protocol, runtime_checkable

from .config import Settings

logger = logging.getLogger("halos.ai.llm")


@runtime_checkable
class LlmClient(Protocol):
    """LLM istemci sözleşmesi. Sistem + kullanıcı promptu alır, metin cevap döner."""

    def answer(self, system: str, user: str) -> str:
        """Verilen sistem/kullanıcı promptuna karşılık metin yanıt üretir."""
        ...


class AnthropicLlmClient:
    """Anthropic Claude (Messages API) tabanlı gerçek LLM istemcisi.

    Yalnızca geçerli bir API anahtarı olduğunda oluşturulur. `anthropic` SDK'sı
    yalnızca burada import edilir; böylece anahtarsız ortamlarda SDK gereksinimi
    zorunlu bir çalışma bağımlılığı olmaktan çıkar (yine de requirements'ta yer alır).
    """

    def __init__(self, api_key: str, model: str, max_tokens: int = 1024) -> None:
        # Geç import: paket testte/anahtarsız ortamda yüklenmese bile modül import olur.
        from anthropic import Anthropic

        self._client = Anthropic(api_key=api_key)
        self._model = model
        self._max_tokens = max_tokens

    @property
    def model(self) -> str:
        return self._model

    def answer(self, system: str, user: str) -> str:
        """Claude Messages API'sini çağırır ve düz metin cevabı döndürür."""
        response = self._client.messages.create(
            model=self._model,
            max_tokens=self._max_tokens,
            system=system,
            messages=[{"role": "user", "content": user}],
        )
        # İçerik blokları metin parçalarına ayrılır; yalnız metin bloklarını birleştir.
        parts: list[str] = []
        for block in response.content:
            text = getattr(block, "text", None)
            if text:
                parts.append(text)
        return "".join(parts).strip()


class OllamaLlmClient:
    """Yerel Ollama (llama/aya vb.) tabanlı LLM istemcisi.

    Bedava, çevrimdışı çalışır ve veri makineden çıkmaz (hal mali verisi için mahremiyet
    avantajı). Ollama'nın yerel /api/chat ucunu httpx ile çağırır. Türkçe için aya-expanse:8b
    önerilir. Claude'a göre kalite daha düşüktür; kalite gerektiren üretim işlerinde Anthropic tercih edilir.
    """

    def __init__(self, base_url: str, model: str, timeout_s: float = 120.0) -> None:
        self._base_url = base_url.rstrip("/")
        self._model = model
        self._timeout = timeout_s

    @property
    def model(self) -> str:
        return self._model

    def answer(self, system: str, user: str) -> str:
        import httpx

        response = httpx.post(
            f"{self._base_url}/api/chat",
            json={
                "model": self._model,
                "messages": [
                    {"role": "system", "content": system},
                    {"role": "user", "content": user},
                ],
                "stream": False,
                "options": {"temperature": 0.2},
            },
            timeout=self._timeout,
        )
        response.raise_for_status()
        data = response.json()
        return (data.get("message", {}).get("content") or "").strip()


class StubLlmClient:
    """Deterministik, dış çağrı içermeyen Türkçe özet üreten yedek istemci.

    Anahtarsız ortam ve testler için. Girdi verisini yansıtan SABİT bir format
    kullanır; aynı girdi her zaman aynı çıktıyı verir (testlerde doğrulanabilir).
    """

    model = "stub"

    def answer(self, system: str, user: str) -> str:
        """Sistem + kullanıcı promptunu yansıtan sabit biçimli Türkçe cevap üretir."""
        return (
            "[HalOS AI muhasebeci — stub yanıt] "
            "Gerçek Claude çağrısı yapılmadı (API anahtarı yapılandırılmamış). "
            "Aşağıdaki yanıt yalnızca sağlanan ERP verisine dayanır.\n\n"
            f"Soru ve veri özeti:\n{user}"
        )


def _build_ollama(settings: Settings) -> LlmClient:
    logger.info(
        "Ollama kullanılacak: %s @ %s (yerel/çevrimdışı LLM).",
        settings.ollama_model,
        settings.ollama_base_url,
    )
    return OllamaLlmClient(base_url=settings.ollama_base_url, model=settings.ollama_model)


def _build_anthropic(settings: Settings) -> LlmClient:
    logger.info("Anthropic Claude (%s) kullanılacak.", settings.anthropic_model)
    return AnthropicLlmClient(
        api_key=settings.anthropic_api_key,  # type: ignore[arg-type]
        model=settings.anthropic_model,
    )


def build_llm_client(settings: Settings) -> LlmClient:
    """LLM_PROVIDER ayarına göre istemci seçer (kararı log'lar).

    - ollama    → yerel Ollama (bedava/çevrimdışı)
    - anthropic → Claude (anahtar gerekir; yoksa Stub)
    - stub      → sahte
    - auto      → anahtar varsa Claude, yoksa Ollama, o da yoksa Stub
    """
    provider = (settings.llm_provider or "auto").strip().lower()

    if provider == "ollama":
        return _build_ollama(settings)
    if provider == "anthropic":
        if settings.has_anthropic_key:
            return _build_anthropic(settings)
        logger.warning("LLM_PROVIDER=anthropic ama anahtar yok; Stub kullanılacak.")
        return StubLlmClient()
    if provider == "stub":
        return StubLlmClient()

    # auto
    if settings.has_anthropic_key:
        return _build_anthropic(settings)
    if settings.ollama_base_url:
        return _build_ollama(settings)
    logger.warning("LLM sağlayıcı çözülemedi; Stub kullanılacak.")
    return StubLlmClient()

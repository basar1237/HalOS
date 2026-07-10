"""AI Gateway uç-uca testleri (anahtarsız / stub).

Kapsananlar (görev gereği):
1) GET /health
2) POST /ai/ask token'sız → 401
3) POST /ai/ask yanlış rol → 403
4) POST /ai/ask geçerli Accountant JWT + Stub ERP + Stub LLM → 200 + answer + used_sources
5) build_accountant_prompt ERP verisini içeriyor
6) build_llm_client anahtarsız Stub döner
"""

from __future__ import annotations

from app.config import Settings
from app.llm import AnthropicLlmClient, StubLlmClient, build_llm_client
from app.prompts import build_accountant_prompt, build_insights_prompt

from .conftest import make_token


# --- (1) sağlık ------------------------------------------------------------
def test_health_returns_ok(client):
    response = client.get("/health")
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "ok"
    # Anahtarsız ortamda LLM stub olmalı.
    assert body["llm"] == "stub"


# --- (2) token yok → 401 ---------------------------------------------------
def test_ask_without_token_is_401(client):
    response = client.post("/ai/ask", json={"question": "Bu ay ne kadar sattık?"})
    assert response.status_code == 401


def test_ask_with_invalid_token_is_401(client):
    response = client.post(
        "/ai/ask",
        json={"question": "Bu ay ne kadar sattık?"},
        headers={"Authorization": "Bearer not-a-real-jwt"},
    )
    assert response.status_code == 401


def test_ask_with_wrong_signing_key_is_401(client):
    bad = make_token(role="Accountant", signing_key="baska-bir-imza-anahtari-yanlis-0000")
    response = client.post(
        "/ai/ask",
        json={"question": "Bu ay ne kadar sattık?"},
        headers={"Authorization": f"Bearer {bad}"},
    )
    assert response.status_code == 401


def test_ask_without_tenant_claim_is_401(client):
    token = make_token(role="Accountant", include_tenant=False)
    response = client.post(
        "/ai/ask",
        json={"question": "Bu ay ne kadar sattık?"},
        headers={"Authorization": f"Bearer {token}"},
    )
    assert response.status_code == 401


# --- (3) yanlış rol → 403 --------------------------------------------------
def test_ask_with_unauthorized_role_is_403(client):
    token = make_token(role="Cashier")  # izinli roller: Owner/Manager/Accountant
    response = client.post(
        "/ai/ask",
        json={"question": "Bu ay ne kadar sattık?"},
        headers={"Authorization": f"Bearer {token}"},
    )
    assert response.status_code == 403


# --- (4) geçerli Accountant → 200 + answer + used_sources ------------------
def test_ask_with_accountant_returns_answer_and_sources(client):
    token = make_token(role="Accountant")
    response = client.post(
        "/ai/ask",
        json={"question": "Bu ayki satış ve cari durumumuz nedir?"},
        headers={"Authorization": f"Bearer {token}"},
    )
    assert response.status_code == 200
    body = response.json()
    assert body["answer"]  # boş olmamalı
    assert body["model"] == "stub"
    assert body["used_sources"] == [
        "sales:/reports/sales-summary",
        "finance:/reports/aging",
    ]
    # Stub yanıt, soruyu (user prompt üzerinden) yansıtmalı.
    assert "satış" in body["answer"] or "cari" in body["answer"]


def test_ask_accepts_owner_and_manager_roles(client):
    for role in ("Owner", "Manager"):
        token = make_token(role=role)
        response = client.post(
            "/ai/ask",
            json={"question": "Durum raporu"},
            headers={"Authorization": f"Bearer {token}"},
        )
        assert response.status_code == 200, role


# --- (5) prompt builder ERP verisini içeriyor ------------------------------
def test_prompt_builder_embeds_erp_data():
    erp_data = {
        "sales_summary": {"totalNet": 112500.50, "currency": "TRY"},
        "aging": {"totalAmount": 81000.00},
    }
    system, user = build_accountant_prompt("Kâr durumu nedir?", erp_data)

    # Sistem promptu: rol + kısıtlar Türkçe.
    assert "muhasebeci" in system.lower()
    assert "UYDURMA" in system or "uydurma" in system.lower()

    # Kullanıcı promptu: soru + ERP verisi (sayılar) gömülü.
    assert "Kâr durumu nedir?" in user
    assert "112500.5" in user
    assert "81000" in user
    assert "TRY" in user


# --- (6) build_llm_client anahtarsız Stub döner ----------------------------
def test_build_llm_client_without_key_returns_stub():
    settings = Settings(anthropic_api_key=None)
    client_obj = build_llm_client(settings)
    assert isinstance(client_obj, StubLlmClient)
    assert client_obj.model == "stub"


def test_build_llm_client_empty_key_returns_stub():
    settings = Settings(anthropic_api_key="   ")  # boşluk → anahtar yok sayılır
    assert isinstance(build_llm_client(settings), StubLlmClient)


# --- (7) proaktif AI ajanı: /ai/insights (docs/06 S3.2) --------------------
def test_insights_without_token_is_401(client):
    response = client.post("/ai/insights", json={})
    assert response.status_code == 401


def test_insights_with_unauthorized_role_is_403(client):
    token = make_token(role="Cashier")
    response = client.post(
        "/ai/insights", json={}, headers={"Authorization": f"Bearer {token}"}
    )
    assert response.status_code == 403


def test_insights_with_accountant_returns_summary_and_sources(client):
    token = make_token(role="Accountant")
    response = client.post(
        "/ai/insights", json={}, headers={"Authorization": f"Bearer {token}"}
    )
    assert response.status_code == 200
    body = response.json()
    assert body["summary"]  # boş olmamalı
    assert body["model"] == "stub"
    assert body["used_sources"] == [
        "sales:/reports/sales-summary",
        "finance:/reports/aging",
    ]


def test_insights_prompt_builder_embeds_erp_data_and_is_proactive():
    erp_data = {
        "sales_summary": {"totalNet": 112500.50, "currency": "TRY"},
        "aging": {"totalAmount": 81000.00, "days31Plus": {"amount": 3000.00}},
    }
    system, user = build_insights_prompt(erp_data)

    # Sistem promptu: proaktif rol + kısıtlar Türkçe.
    assert "proaktif" in system.lower()
    assert "UYDURMA" in system or "uydurma" in system.lower()
    # Kullanıcı promptu: ERP verisi (sayılar) gömülü.
    assert "112500.5" in user
    assert "81000" in user


def test_build_llm_client_with_key_returns_anthropic():
    # Not: sahte anahtarla Anthropic istemcisi OLUŞTURULUR ama hiçbir ağ çağrısı YAPILMAZ.
    settings = Settings(anthropic_api_key="sk-ant-test-fake-key")
    client_obj = build_llm_client(settings)
    assert isinstance(client_obj, AnthropicLlmClient)
    assert client_obj.model == "claude-sonnet-4-6"

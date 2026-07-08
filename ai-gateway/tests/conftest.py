"""Test yardımcıları: JWT üretici + stub'lı TestClient fabrikası.

Gerçek Anthropic anahtarı veya çalışan ERP GEREKMEZ; her şey DI ile stub'lanır.
"""

from __future__ import annotations

from datetime import datetime, timedelta, timezone

import jwt
import pytest
from fastapi.testclient import TestClient

from app.auth import ROLE_CLAIM, TENANT_CLAIM
from app.config import Settings, get_settings
from app.erp_client import StubErpReadClient
from app.llm import StubLlmClient
from app.main import app, get_erp_client, get_llm_client

# Testlerde kullanılan sabit imza anahtarı (Settings varsayılanıyla aynı).
TEST_SIGNING_KEY = "dev-only-signing-key-change-me-please-0123456789abcdef"
TEST_ISSUER = "HalOS.Identity"
TEST_AUDIENCE = "HalOS"


def make_token(
    role: str = "Accountant",
    tenant_id: str = "11111111-1111-1111-1111-111111111111",
    *,
    signing_key: str = TEST_SIGNING_KEY,
    include_tenant: bool = True,
) -> str:
    """ERP Identity'nin ürettiğine benzer bir HS256 JWT üretir."""
    now = datetime.now(timezone.utc)
    payload: dict[str, object] = {
        "sub": "user-123",
        ROLE_CLAIM: role,
        "iss": TEST_ISSUER,
        "aud": TEST_AUDIENCE,
        "iat": now,
        "exp": now + timedelta(hours=1),
    }
    if include_tenant:
        payload[TENANT_CLAIM] = tenant_id
    return jwt.encode(payload, signing_key, algorithm="HS256")


@pytest.fixture
def stub_settings() -> Settings:
    """Anahtarsız (Anthropic anahtarı olmayan) deterministik ayarlar."""
    return Settings(
        anthropic_api_key=None,
        jwt={"signing_key": TEST_SIGNING_KEY, "issuer": TEST_ISSUER, "audience": TEST_AUDIENCE},
    )


@pytest.fixture
def client(stub_settings: Settings) -> TestClient:
    """Stub LLM + Stub ERP + anahtarsız ayarlarla enjekte edilmiş TestClient."""
    app.dependency_overrides[get_settings] = lambda: stub_settings
    app.dependency_overrides[get_llm_client] = lambda: StubLlmClient()
    app.dependency_overrides[get_erp_client] = lambda: StubErpReadClient()
    with TestClient(app) as test_client:
        yield test_client
    app.dependency_overrides.clear()

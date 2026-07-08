"""JWT imza anahtarı fail-fast politikası testleri (ADR-002, docs/07 §güvenlik).

Non-development ortamda eksik/zayıf/repoya işlenmiş varsayılan anahtar REDDEDİLMELİ;
development'ta ise geliştirme kolaylığı için kabul edilmeli. Bu, .NET tarafındaki
JwtSigningKeyResolver.Resolve(..., IsDevelopment()) davranışıyla simetriktir.

Testler ortam değişkeni GEREKTİRMEZ: `validate_signing_key` doğrudan çağrılır; ayrıca
`_is_development` ve cache'li `get_settings` için ortam davranışı monkeypatch ile denetlenir.
"""

from __future__ import annotations

import pytest

from app import config
from app.config import (
    DEV_ONLY_SIGNING_KEY,
    MINIMUM_KEY_LENGTH_BYTES,
    Settings,
    _is_development,
    get_settings,
)

# 32 bayttan uzun, dev varsayılanı OLMAYAN güçlü örnek anahtar.
STRONG_KEY = "x" * (MINIMUM_KEY_LENGTH_BYTES + 8)


def _settings_with_key(signing_key: str) -> Settings:
    return Settings(
        anthropic_api_key=None,
        jwt={"signing_key": signing_key, "issuer": "HalOS.Identity", "audience": "HalOS"},
    )


# --- development: her şey kabul (dev kolaylığı) -----------------------------
def test_dev_accepts_committed_default():
    """Development'ta repoya işlenmiş varsayılan anahtar kabul edilir (hata YOK)."""
    _settings_with_key(DEV_ONLY_SIGNING_KEY).validate_signing_key(is_development=True)


def test_dev_accepts_short_key():
    """Development'ta çok kısa anahtar bile kabul edilir."""
    _settings_with_key("kisa").validate_signing_key(is_development=True)


# --- non-development: fail-fast ---------------------------------------------
def test_prod_rejects_committed_default():
    """Non-development ortamda bilinen dev varsayılanı → RuntimeError (kimlik sınırı korunur)."""
    settings = _settings_with_key(DEV_ONLY_SIGNING_KEY)
    with pytest.raises(RuntimeError) as exc:
        settings.validate_signing_key(is_development=False)
    assert "JWT__SIGNING_KEY" in str(exc.value)


def test_prod_rejects_short_key():
    """Non-development ortamda 32 bayttan kısa anahtar → RuntimeError."""
    with pytest.raises(RuntimeError):
        _settings_with_key("kisa-anahtar").validate_signing_key(is_development=False)


def test_prod_rejects_empty_key():
    """Non-development ortamda boş anahtar → RuntimeError."""
    with pytest.raises(RuntimeError):
        _settings_with_key("").validate_signing_key(is_development=False)


def test_prod_accepts_strong_custom_key():
    """Non-development ortamda güçlü (>=32 bayt, varsayılan olmayan) anahtar kabul edilir."""
    _settings_with_key(STRONG_KEY).validate_signing_key(is_development=False)


# --- ortam tespiti (_is_development) ----------------------------------------
def test_is_development_defaults_to_production_when_unset(monkeypatch):
    """Hiçbir ortam değişkeni yoksa GÜVENLİ VARSAYILAN Production (development DEĞİL)."""
    for var in ("APP_ENV", "ENVIRONMENT", "ASPNETCORE_ENVIRONMENT"):
        monkeypatch.delenv(var, raising=False)
    assert _is_development() is False


def test_is_development_app_env_true(monkeypatch):
    for var in ("ENVIRONMENT", "ASPNETCORE_ENVIRONMENT"):
        monkeypatch.delenv(var, raising=False)
    monkeypatch.setenv("APP_ENV", "Development")
    assert _is_development() is True


def test_is_development_aspnetcore_fallback(monkeypatch):
    monkeypatch.delenv("APP_ENV", raising=False)
    monkeypatch.delenv("ENVIRONMENT", raising=False)
    monkeypatch.setenv("ASPNETCORE_ENVIRONMENT", "development")
    assert _is_development() is True


def test_app_env_precedence_over_aspnetcore(monkeypatch):
    """APP_ENV, ASPNETCORE_ENVIRONMENT'tan önceliklidir."""
    monkeypatch.delenv("ENVIRONMENT", raising=False)
    monkeypatch.setenv("APP_ENV", "Production")
    monkeypatch.setenv("ASPNETCORE_ENVIRONMENT", "Development")
    assert _is_development() is False


# --- get_settings entegrasyonu (fail-fast yükleme sırasında) ----------------
def test_get_settings_fails_fast_in_production_with_default_key(monkeypatch):
    """Production ortamında varsayılan anahtarla get_settings() → RuntimeError."""
    for var in ("APP_ENV", "ENVIRONMENT", "ASPNETCORE_ENVIRONMENT"):
        monkeypatch.delenv(var, raising=False)
    monkeypatch.setenv("APP_ENV", "Production")
    monkeypatch.delenv("JWT__SIGNING_KEY", raising=False)  # varsayılan devreye girsin
    get_settings.cache_clear()
    try:
        with pytest.raises(RuntimeError):
            get_settings()
    finally:
        get_settings.cache_clear()


def test_get_settings_ok_in_production_with_strong_key(monkeypatch):
    """Production ortamında güçlü anahtar verilirse get_settings() sorunsuz döner."""
    monkeypatch.setenv("APP_ENV", "Production")
    monkeypatch.setenv("JWT__SIGNING_KEY", STRONG_KEY)
    get_settings.cache_clear()
    try:
        settings = get_settings()
        assert settings.jwt.signing_key == STRONG_KEY
    finally:
        get_settings.cache_clear()


def test_get_settings_ok_in_development_with_default_key(monkeypatch):
    """Development ortamında varsayılan anahtarla get_settings() sorunsuz döner."""
    monkeypatch.setenv("APP_ENV", "development")
    monkeypatch.delenv("JWT__SIGNING_KEY", raising=False)
    get_settings.cache_clear()
    try:
        settings = get_settings()
        assert settings.jwt.signing_key == config.DEV_ONLY_SIGNING_KEY
    finally:
        get_settings.cache_clear()

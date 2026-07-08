"""Uygulama yapılandırması (pydantic-settings).

Tüm sırlar ortam değişkeninden gelir; koda sır yazılmaz (docs/04 §7, docs/07 ruhu).
- Anthropic API anahtarı OPSİYONELDİR: yoksa servis Stub LLM ile anahtarsız çalışır
  (bkz. app.llm.build_llm_client). Anahtar istenmez / uydurulmaz.
- JWT ayarları ERP (Identity) ile AYNI imza anahtarı/issuer/audience üzerine kuruludur;
  böylece Gateway, ERP'nin ürettiği token'ları doğrular (ADR-002).
- ERP taban URL'leri (Sales/Finance) SALT-OKUMA rapor uçlarına erişim içindir.

Ortam değişkeni adlandırması .NET servisleriyle uyumlu tutulur (çift alt çizgi ile
iç içe alan): örn. `JWT__SIGNING_KEY`, `ERP__SALES_BASE_URL`.
"""

from __future__ import annotations

import os
from functools import lru_cache

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict

# --- Güvenlik: JWT imza anahtarı politikası (docs/07 §güvenlik, ADR-002) ---
# Development DIŞINDA eksik/zayıf/tahmin edilebilir anahtar KABUL EDİLMEZ → fail-fast.
# Bu, .NET servislerindeki JwtSigningKeyResolver.Resolve(..., IsDevelopment()) mantığının
# Python karşılığıdır; böylece Gateway ile .NET servisleri arasında asimetri oluşmaz.

# Repoya işlenmiş, herkesçe bilinen geliştirme anahtarı. YALNIZ development'ta kabul edilir.
# Üretimde bu değer kullanılırsa saldırgan geçerli Owner token üretip tüm tenant
# raporlarını okuyabilir; bu yüzden non-development ortamda REDDEDİLİR.
DEV_ONLY_SIGNING_KEY = "dev-only-signing-key-change-me-please-0123456789abcdef"

# HS256 için asgari anahtar uzunluğu (256 bit = 32 bayt) — RFC 7518, .NET tarafıyla birebir.
MINIMUM_KEY_LENGTH_BYTES = 32


def _is_development() -> bool:
    """Ortamın "development" olup olmadığını .NET'e uyumlu biçimde belirler.

    Öncelik sırası:
    - `APP_ENV` (proje standardı) veya yoksa `ENVIRONMENT`; her ikisi de yoksa
      `ASPNETCORE_ENVIRONMENT` (mevcut .NET servisleriyle aynı değişken adı).
    - Hiçbiri verilmemişse GÜVENLİ VARSAYILAN olarak "Production" kabul edilir
      (fail-safe): ortam belirtilmediğinde zayıf/varsayılan anahtar REDDEDİLİR.
    """
    env = (
        os.getenv("APP_ENV")
        or os.getenv("ENVIRONMENT")
        or os.getenv("ASPNETCORE_ENVIRONMENT")
        or "Production"
    )
    return env.strip().lower() == "development"


class JwtSettings(BaseSettings):
    """JWT doğrulama ayarları (HS256). ERP Identity servisiyle aynı değerler.

    docs/04 §7: token'da `tenant_id` ve `role` claim'leri bulunur; issuer
    `HalOS.Identity`, audience `HalOS`. İmza anahtarı ERP ile paylaşılır.

    Güvenlik: `signing_key` alanının bir geliştirme varsayılanı vardır; ancak bu
    varsayılan (veya çok kısa bir anahtar) YALNIZ development ortamında kabul edilir.
    Non-development ortamda doğrulama için bkz. `Settings.validate_signing_key`.
    """

    model_config = SettingsConfigDict(env_prefix="JWT__", extra="ignore")

    # HS256 imza anahtarı — ERP ile AYNI. Development dışında zorunludur.
    signing_key: str = Field(
        default=DEV_ONLY_SIGNING_KEY,
        description="ERP ile paylaşılan HS256 imza anahtarı (JWT__SIGNING_KEY).",
    )
    issuer: str = Field(default="HalOS.Identity")
    audience: str = Field(default="HalOS")


class ErpSettings(BaseSettings):
    """ERP servislerine SALT-OKUMA erişim ayarları (ADR-002).

    Gateway yalnızca rapor (okuma) uçlarını çağırır; hiçbir yazma yapmaz.
    Opsiyonel `service_token`, kullanıcı JWT'si taşınamadığında (örn. arka plan
    işleri) kullanılacak servis kimliği içindir; normal akışta gelen kullanıcı
    JWT'si ERP'ye iletilir.
    """

    model_config = SettingsConfigDict(env_prefix="ERP__", extra="ignore")

    sales_base_url: str = Field(
        default="http://localhost:5055",
        description="Sales servisi taban URL'i (rapor uçları: /reports/*).",
    )
    finance_base_url: str = Field(
        default="http://localhost:5065",
        description="Finance servisi taban URL'i (rapor uçları: /reports/*).",
    )
    service_token: str | None = Field(
        default=None,
        description="Opsiyonel servis-servis çağrıları için Bearer token.",
    )
    request_timeout_seconds: float = Field(default=10.0)


class Settings(BaseSettings):
    """Kök yapılandırma. Ortam değişkeninden yüklenir (.env desteklenir)."""

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
        # Alan adıyla da (alias'a ek olarak) doldurulabilsin: örn. Settings(anthropic_api_key=...).
        populate_by_name=True,
    )

    # --- Anthropic / Claude ---
    # Anahtar OPSİYONELDİR. Yoksa Stub LLM devreye girer (anahtar istenmez).
    anthropic_api_key: str | None = Field(
        default=None,
        alias="ANTHROPIC_API_KEY",
        description="Anthropic API anahtarı. Boşsa Stub LLM kullanılır.",
    )
    anthropic_model: str = Field(
        default="claude-sonnet-4-6",
        alias="ANTHROPIC_MODEL",
        description="Varsayılan Claude modeli (Messages API).",
    )

    # --- Alt ayar grupları ---
    jwt: JwtSettings = Field(default_factory=JwtSettings)
    erp: ErpSettings = Field(default_factory=ErpSettings)

    @property
    def has_anthropic_key(self) -> bool:
        """Gerçek Claude çağrısı için anahtar mevcut mu?"""
        return bool(self.anthropic_api_key and self.anthropic_api_key.strip())

    def validate_signing_key(self, *, is_development: bool) -> None:
        """JWT imza anahtarını ortam politikasına göre doğrular (fail-fast).

        .NET `JwtSigningKeyResolver.Resolve` mantığının Python karşılığı:
        - Anahtar >= 32 bayt VE bilinen dev varsayılanı DEĞİLSE → geçerli (her ortamda).
        - Aksi halde (eksik/çok kısa VEYA repoya işlenmiş dev anahtarı):
          * development → kabul edilir (yerel geliştirme kolaylığı),
          * non-development → RuntimeError ile fail-fast.

        Böylece JWT__SIGNING_KEY hiç verilmese bile servis, üretimde tahmin edilebilir
        anahtarla sessizce token doğrulamaya DEVAM ETMEZ (ADR-002 kimlik sınırı korunur).
        """
        if is_development:
            return

        key = self.jwt.signing_key or ""
        byte_length = len(key.encode("utf-8"))
        is_weak = byte_length < MINIMUM_KEY_LENGTH_BYTES
        is_committed_default = key == DEV_ONLY_SIGNING_KEY

        if is_weak or is_committed_default:
            reason = (
                "repoya işlenmiş geliştirme varsayılanına eşit"
                if is_committed_default
                else f"çok kısa (en az {MINIMUM_KEY_LENGTH_BYTES} bayt gerekli, "
                f"gelen {byte_length} bayt)"
            )
            raise RuntimeError(
                "JWT imza anahtarı (JWT__SIGNING_KEY) non-development ortamda geçersiz: "
                f"{reason}. Güçlü ve gizli bir anahtarı ortam değişkeni/Vault üzerinden "
                "sağlayın. (Bu koruma .NET servisleriyle simetriktir; docs/07 §güvenlik.)"
            )


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    """Süreç boyu tekil (cache'li) Settings örneği döndürür (FastAPI DI için).

    Yükleme sırasında JWT imza anahtarı ortam politikasına göre doğrulanır:
    non-development ortamda eksik/zayıf/varsayılan anahtar → RuntimeError (fail-fast).
    """
    settings = Settings()
    settings.validate_signing_key(is_development=_is_development())
    return settings

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


def build_llm_client(settings: Settings) -> LlmClient:
    """Anahtar varsa Anthropic, yoksa Stub istemcisi döndürür (kararı log'lar)."""
    if settings.has_anthropic_key:
        logger.info("Anthropic API anahtarı bulundu; Claude (%s) kullanılacak.", settings.anthropic_model)
        return AnthropicLlmClient(
            api_key=settings.anthropic_api_key,  # type: ignore[arg-type]
            model=settings.anthropic_model,
        )
    logger.warning(
        "Anthropic API anahtarı yok; StubLlmClient kullanılacak "
        "(servis anahtarsız çalışır, gerçek LLM çağrısı YAPILMAZ)."
    )
    return StubLlmClient()

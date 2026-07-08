"""ERP SALT-OKUMA istemcisi (ADR-002 güvenlik sınırı).

Gateway, ERP'den YALNIZCA OKUR. Bu modülde HİÇBİR yazma metodu yoktur.
- `ErpReadClient` protokolü: yalnız okuma metotları (satış özeti, cari yaşlandırma).
- `HttpErpReadClient`: httpx ile ERP `/reports/*` uçlarını çağırır; kullanıcının
  JWT'sini `Authorization: Bearer <token>` olarak iletir. Tenant, token içindeki
  claim üzerinden ERP tarafında zaten kapsanır (global query filter, BK-8); Gateway
  ayrıca tenant'ı doğrular ve kayıt/loglama için taşır.
- `StubErpReadClient`: test / yapılandırmasız ortam için sabit, deterministik veri.

Rapor uçları (.NET ReportsController ile hizalı):
- Sales:   GET /reports/sales-summary?from=<ISO>&to=<ISO>
- Finance: GET /reports/aging?asOf=<ISO>
"""

from __future__ import annotations

from datetime import date, datetime
from typing import Any, Protocol, runtime_checkable

import httpx


class ErpUnavailableError(RuntimeError):
    """ERP'ye erişilemediğinde (ağ hatası / HTTP hata kodu) fırlatılır."""


def _to_iso(value: Any) -> str:
    """Tarih/tarih-saat/str değerini ISO 8601 string'e çevirir."""
    if isinstance(value, (datetime, date)):
        return value.isoformat()
    return str(value)


@runtime_checkable
class ErpReadClient(Protocol):
    """ERP okuma sözleşmesi — YALNIZCA okuma. Yazma metodu tanımlanmaz."""

    def get_sales_summary(
        self, tenant_id: str, token: str, date_from: Any, date_to: Any
    ) -> dict[str, Any]:
        """Satış özet raporunu döndürür (Sales /reports/sales-summary)."""
        ...

    def get_aging(self, tenant_id: str, token: str, as_of: Any) -> dict[str, Any]:
        """Cari yaşlandırma raporunu döndürür (Finance /reports/aging)."""
        ...


class HttpErpReadClient:
    """httpx tabanlı gerçek ERP okuma istemcisi (SALT-OKUMA)."""

    def __init__(
        self,
        sales_base_url: str,
        finance_base_url: str,
        timeout_seconds: float = 10.0,
        service_token: str | None = None,
    ) -> None:
        self._sales_base_url = sales_base_url.rstrip("/")
        self._finance_base_url = finance_base_url.rstrip("/")
        self._timeout = timeout_seconds
        self._service_token = service_token

    def _headers(self, token: str) -> dict[str, str]:
        """Yetkilendirme başlığı. Kullanıcı token'ı yoksa servis token'ına düşer."""
        bearer = token or self._service_token or ""
        return {"Authorization": f"Bearer {bearer}", "Accept": "application/json"}

    def _get(self, base_url: str, path: str, token: str, params: dict[str, str]) -> dict[str, Any]:
        """Ortak GET yardımcı — hata durumunda ErpUnavailableError'a çevirir."""
        url = f"{base_url}{path}"
        try:
            response = httpx.get(
                url,
                params=params,
                headers=self._headers(token),
                timeout=self._timeout,
            )
            response.raise_for_status()
            return response.json()
        except httpx.HTTPStatusError as exc:
            raise ErpUnavailableError(
                f"ERP raporu hata döndürdü ({exc.response.status_code}): {url}"
            ) from exc
        except httpx.HTTPError as exc:
            raise ErpUnavailableError(f"ERP'ye erişilemedi: {url} ({exc})") from exc

    def get_sales_summary(
        self, tenant_id: str, token: str, date_from: Any, date_to: Any
    ) -> dict[str, Any]:
        return self._get(
            self._sales_base_url,
            "/reports/sales-summary",
            token,
            {"from": _to_iso(date_from), "to": _to_iso(date_to)},
        )

    def get_aging(self, tenant_id: str, token: str, as_of: Any) -> dict[str, Any]:
        return self._get(
            self._finance_base_url,
            "/reports/aging",
            token,
            {"asOf": _to_iso(as_of)},
        )


class StubErpReadClient:
    """Sabit, deterministik veri döndüren test/yapılandırmasız istemci (SALT-OKUMA).

    Gerçek ERP veya ağ gerektirmez; DI ile enjekte edilerek testlerde ve anahtarsız
    demolarda kullanılır.
    """

    def get_sales_summary(
        self, tenant_id: str, token: str, date_from: Any, date_to: Any
    ) -> dict[str, Any]:
        return {
            "from": _to_iso(date_from),
            "to": _to_iso(date_to),
            "count": 42,
            "totalGross": 125000.50,
            "totalCommission": 10000.00,
            "totalDeductions": 2500.00,
            "totalNet": 112500.50,
            "currency": "TRY",
        }

    def get_aging(self, tenant_id: str, token: str, as_of: Any) -> dict[str, Any]:
        return {
            "asOf": _to_iso(as_of),
            "current": {"amount": 50000.00, "accountCount": 12},
            "days0To15": {"amount": 20000.00, "accountCount": 5},
            "days16To30": {"amount": 8000.00, "accountCount": 3},
            "days31Plus": {"amount": 3000.00, "accountCount": 1},
            "totalAmount": 81000.00,
            "totalAccountCount": 21,
            "currency": "TRY",
        }

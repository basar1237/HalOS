"""JWT doğrulama ve yetki (ADR-002, docs/04 §7).

Gelen Bearer token'ı ERP (Identity) ile AYNI HS256 imza anahtarıyla doğrular; issuer
`HalOS.Identity`, audience `HalOS`. Token'dan `tenant_id` ve `role` claim'lerini çıkarır
(docs/04 §7 claim adları). Rol Owner/Manager/Accountant değilse 403 döner.

- Geçersiz/eksik token → 401
- Yetkisiz rol → 403
- `require_accountant` FastAPI dependency'si doğrulanmış `Principal` döndürür.
"""

from __future__ import annotations

from dataclasses import dataclass

import jwt
from fastapi import Depends, HTTPException, Request, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

from .config import Settings, get_settings

# docs/04 §7 claim adları — Identity servisiyle birebir aynı.
TENANT_CLAIM = "tenant_id"
ROLE_CLAIM = "role"

# Rapor okuma yetkisine sahip roller (docs/03 §3 RBAC; Finance/Sales ReportRead ile hizalı).
ALLOWED_ROLES: frozenset[str] = frozenset({"Owner", "Manager", "Accountant"})

# auto_error=False: başlık yoksa kendi 401'imizi (WWW-Authenticate ile) döndürebiliriz.
_bearer_scheme = HTTPBearer(auto_error=False)


@dataclass(frozen=True)
class Principal:
    """Doğrulanmış çağıran kimliği: tenant, rol ve ham token (ERP'ye iletmek için)."""

    tenant_id: str
    role: str
    token: str


def _unauthorized(detail: str) -> HTTPException:
    return HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail=detail,
        headers={"WWW-Authenticate": "Bearer"},
    )


def decode_token(token: str, settings: Settings) -> dict:
    """Token'ı doğrular ve claim sözlüğü döndürür; geçersizse 401 fırlatır."""
    try:
        return jwt.decode(
            token,
            settings.jwt.signing_key,
            algorithms=["HS256"],
            issuer=settings.jwt.issuer,
            audience=settings.jwt.audience,
        )
    except jwt.PyJWTError as exc:
        raise _unauthorized(f"Geçersiz veya süresi dolmuş token: {exc}") from exc


def require_accountant(
    request: Request,
    credentials: HTTPAuthorizationCredentials | None = Depends(_bearer_scheme),
    settings: Settings = Depends(get_settings),
) -> Principal:
    """Muhasebe erişimi için FastAPI dependency'si.

    1) Bearer token yoksa/boşsa → 401
    2) İmza/issuer/audience geçersizse → 401
    3) `tenant_id` claim'i yoksa → 401 (tenant kapsaması olmadan sorgu yapılamaz)
    4) `role` izinli değilse (Owner/Manager/Accountant dışı) → 403

    Not: `request` parametresi, ileride ihtiyaç duyulabilecek istek bağlamı (ör.
    correlation id) için imzada tutulur; şu an doğrudan kullanılmaz.
    """
    if credentials is None or not credentials.credentials:
        raise _unauthorized("Yetkilendirme başlığı (Bearer token) eksik.")

    token = credentials.credentials
    claims = decode_token(token, settings)

    tenant_id = claims.get(TENANT_CLAIM)
    if not tenant_id:
        raise _unauthorized("Token içinde tenant_id claim'i bulunamadı.")

    role = claims.get(ROLE_CLAIM)
    if role not in ALLOWED_ROLES:
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail=(
                f"Bu işlem için yetki yok. Gerekli rol: {sorted(ALLOWED_ROLES)}; "
                f"gelen rol: {role!r}."
            ),
        )

    return Principal(tenant_id=str(tenant_id), role=str(role), token=token)

// Paylaşılan tipler (docs/07 §2: shared — tip/bileşen paylaşımı mobil ile).
// İsimler docs/02 Ortak Sözlük'teki İngilizce kod adlarından.

/** Kimlik & Tenant bağlamı — docs/02 §1, ADR-008/009. */
export interface Tenant {
  /** Multi-tenant izolasyon anahtarı (docs/07 §6, BK-8). */
  id: string;
  /** İşletmenin görünen adı. */
  name: string;
}

/** Oturum açmış kullanıcı (docs/02: User). */
export interface User {
  id: string;
  fullName: string;
  email: string;
  /** RBAC rolleri (docs/03 §3, ileride genişler). */
  roles: string[];
}

/** JWT + refresh çifti — ADR-009. */
export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

/** Login formu girdisi. 2FA etkinse twoFactorCode zorunlu (Identity döner). */
export interface LoginCredentials {
  email: string;
  password: string;
  twoFactorCode?: string;
}

/**
 * Identity /auth/login yanıtı — backend AuthenticationResult ile birebir (camelCase JSON).
 */
export interface AuthenticationResult {
  accessToken: string;
  accessTokenExpiresOnUtc: string;
  refreshToken: string;
  refreshTokenExpiresOnUtc: string;
  userId: string;
  tenantId: string;
  email: string;
  role: string;
}

/**
 * Identity /me yanıtı — backend CurrentUserDto ile birebir. Oturum kurtarmada kullanıcıyı çözer.
 */
export interface CurrentUserDto {
  id: string;
  tenantId: string;
  email: string;
  fullName: string;
  role: string;
  twoFactorEnabled: boolean;
}

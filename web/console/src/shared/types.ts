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

/** Login formu girdisi. */
export interface LoginCredentials {
  email: string;
  password: string;
}

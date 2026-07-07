// Token saklama yeri — ADR-009 (JWT + refresh).
// NOT (iskelet): Kalıcı saklama stratejisi (httpOnly cookie'ler tercih edilir) henüz
// karara bağlanmadı. Bu faz için basit, çerçeveden bağımsız bir soyutlama sunuyoruz;
// gerçek uçtan uca akış ileriki fazda Identity servisiyle bağlanacak.

import type { AuthTokens } from '@/shared/types';

const ACCESS_TOKEN_KEY = 'halos.accessToken';
const REFRESH_TOKEN_KEY = 'halos.refreshToken';

function isBrowser(): boolean {
  return typeof window !== 'undefined';
}

export function saveTokens(tokens: AuthTokens): void {
  if (!isBrowser()) return;
  window.localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
  window.localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
}

export function getAccessToken(): string | null {
  if (!isBrowser()) return null;
  return window.localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken(): string | null {
  if (!isBrowser()) return null;
  return window.localStorage.getItem(REFRESH_TOKEN_KEY);
}

export function clearTokens(): void {
  if (!isBrowser()) return;
  window.localStorage.removeItem(ACCESS_TOKEN_KEY);
  window.localStorage.removeItem(REFRESH_TOKEN_KEY);
}

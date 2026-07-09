// Token saklama — expo-secure-store (iOS Keychain / Android Keystore; web'de localStorage fallback).
// ADR-009 JWT + refresh. Mobilde güvenli saklama zorunlu (cihaz kaybı senaryosu).

import * as SecureStore from 'expo-secure-store';

const ACCESS = 'halos.accessToken';
const REFRESH = 'halos.refreshToken';

export async function saveTokens(accessToken: string, refreshToken: string): Promise<void> {
  await SecureStore.setItemAsync(ACCESS, accessToken);
  await SecureStore.setItemAsync(REFRESH, refreshToken);
}

export function getAccessToken(): Promise<string | null> {
  return SecureStore.getItemAsync(ACCESS);
}

export async function clearTokens(): Promise<void> {
  await SecureStore.deleteItemAsync(ACCESS);
  await SecureStore.deleteItemAsync(REFRESH);
}

// API client — tüm istekler API Gateway'e (EXPO_PUBLIC_API_BASE_URL) gider; yol öneki servise
// göre (/api/{servis}). JWT'yi async secure-store'dan ekler. RFC7807 ProblemDetails hata çözümü.
// NOT: Gerçek cihazda localhost host makineye ulaşmaz → EXPO_PUBLIC_API_BASE_URL host LAN IP olmalı.

import { getAccessToken } from './token';

const API_BASE_URL =
  process.env.EXPO_PUBLIC_API_BASE_URL ?? 'http://localhost:5000';

export interface ApiError {
  status: number;
  message: string;
  code?: string;
}

export function isApiError(value: unknown): value is ApiError {
  return (
    typeof value === 'object' &&
    value !== null &&
    'status' in value &&
    'message' in value
  );
}

interface RequestOptions {
  method?: string;
  body?: unknown;
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const token = await getAccessToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    let message = response.statusText;
    let code: string | undefined;
    try {
      const problem = (await response.json()) as {
        detail?: string;
        title?: string;
        message?: string;
      };
      message = problem?.detail ?? problem?.message ?? message;
      code = problem?.title;
    } catch {
      // gövde JSON değilse statusText ile devam et
    }
    const error: ApiError = { status: response.status, message, code };
    throw error;
  }

  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: 'GET' }),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body }),
};

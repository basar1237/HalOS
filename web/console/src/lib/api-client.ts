// Basit API client — API Gateway (BFF) ile konuşan fetch sarmalayıcı.
// docs/04 §3: Web Konsol -> API Gateway. ADR-009: her istekte JWT header yeri.

import { getAccessToken } from '@/lib/token-storage';

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5000';

export interface ApiError {
  status: number;
  message: string;
}

interface RequestOptions extends Omit<RequestInit, 'body'> {
  /** JSON gövde; otomatik serileştirilir. */
  body?: unknown;
  /** Tenant claim JWT'de taşınır; ayrıca açık tenant başlığı gerekiyorsa buraya. */
  tenantId?: string;
}

function buildHeaders(options: RequestOptions): Headers {
  const headers = new Headers(options.headers);
  headers.set('Accept', 'application/json');

  if (options.body !== undefined) {
    headers.set('Content-Type', 'application/json');
  }

  // JWT header yeri (ADR-009). Token yoksa header eklenmez;
  // korumalı uçlar 401 döner -> auth akışı yönlendirir.
  const accessToken = getAccessToken();
  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`);
  }

  // Multi-tenant (docs/07 §6): tenant esas olarak JWT claim'inden çözülür.
  // Gerekirse açık başlık de gönderilebilir.
  if (options.tenantId) {
    headers.set('X-Tenant-Id', options.tenantId);
  }

  return headers;
}

async function request<TResponse>(
  path: string,
  options: RequestOptions = {},
): Promise<TResponse> {
  const { body, tenantId, ...rest } = options;

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...rest,
    headers: buildHeaders(options),
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) {
    let message = response.statusText;
    try {
      const problem = (await response.json()) as { message?: string };
      if (problem?.message) message = problem.message;
    } catch {
      // gövde JSON değilse statusText ile devam et
    }
    const error: ApiError = { status: response.status, message };
    throw error;
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}

export const apiClient = {
  get: <T>(path: string, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'GET' }),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'POST', body }),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'PUT', body }),
  delete: <T>(path: string, options?: RequestOptions) =>
    request<T>(path, { ...options, method: 'DELETE' }),
};

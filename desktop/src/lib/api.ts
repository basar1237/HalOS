// API Gateway istemcisi (BFF). Tüm istekler Gateway'e gider; yol öneki servise göre
// (/api/{servis}). Bağlantı yoksa (offline) fetch reddeder → çağıran yakalar ve kuyrukta bekletir.
// Masaüstünde taban URL yapılandırılabilir (VITE_API_BASE_URL); varsayılan yerel Gateway.

import type { CachedParty, CachedProduct, OutboxEntry } from './types';

const API_BASE_URL =
  (import.meta as { env?: Record<string, string> }).env?.VITE_API_BASE_URL ??
  'http://localhost:5000';

export interface ApiError {
  status: number;
  message: string;
  code?: string;
}

let accessToken: string | null = null;

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

async function request<T>(
  path: string,
  options: { method?: string; body?: unknown } = {},
): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.body !== undefined) headers['Content-Type'] = 'application/json';
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`;

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
      // gövde JSON değil → statusText ile devam
    }
    const err: ApiError = { status: response.status, message, code };
    throw err;
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export interface LoginResult {
  accessToken: string;
  tenantId?: string;
  userName?: string;
}

export const gatewayApi = {
  async login(email: string, password: string, tenantId?: string): Promise<LoginResult> {
    const body: Record<string, unknown> = { email, password };
    if (tenantId) body.tenantId = tenantId;
    const res = await request<{ accessToken?: string; token?: string; tenantId?: string; userName?: string }>(
      '/api/identity/auth/login',
      { method: 'POST', body },
    );
    const token = res.accessToken ?? res.token ?? '';
    return { accessToken: token, tenantId: res.tenantId, userName: res.userName };
  },

  /**
   * Bir outbox işlemini buluta oynatır. operationId sunucuda idempotency sağlar
   * (aynı işlemi ikinci kez göndermek çift kayıt oluşturmaz).
   */
  async pushOperation(entry: OutboxEntry): Promise<{ serverId?: string }> {
    switch (entry.opType) {
      case 'create-sale': {
        const res = await request<{ id?: string; saleId?: string }>('/api/sales/sales/offline-sync', {
          method: 'POST',
          body: { operationId: entry.operationId, ...(entry.payload as object) },
        });
        return { serverId: res.id ?? res.saleId };
      }
      case 'cancel-sale': {
        const p = entry.payload as { serverId: string };
        await request(`/api/sales/sales/${p.serverId}/cancel`, {
          method: 'POST',
          body: { operationId: entry.operationId },
        });
        return {};
      }
      default:
        throw { status: 400, message: `Bilinmeyen işlem türü: ${entry.opType}` } satisfies ApiError;
    }
  },

  async pullProducts(): Promise<CachedProduct[]> {
    const res = await request<{ items?: RawProduct[] } | RawProduct[]>(
      '/api/inventory/products?pageSize=500',
    );
    const items = Array.isArray(res) ? res : (res.items ?? []);
    return items.map(mapProduct);
  },

  async pullParties(): Promise<CachedParty[]> {
    const res = await request<{ items?: RawParty[] } | RawParty[]>(
      '/api/party/parties?pageSize=500',
    );
    const items = Array.isArray(res) ? res : (res.items ?? []);
    return items.map(mapParty);
  },
};

interface RawProduct {
  id: string;
  name: string;
  defaultUnit?: number;
  rowVersion?: string;
  updatedAt?: string;
}
interface RawParty {
  id: string;
  name: string;
  type?: number;
  partyType?: number;
  rowVersion?: string;
  updatedAt?: string;
}

function mapProduct(p: RawProduct): CachedProduct {
  return {
    id: p.id,
    name: p.name,
    defaultUnit: (p.defaultUnit ?? 1) as CachedProduct['defaultUnit'],
    rowVersion: p.rowVersion ?? null,
    updatedAt: p.updatedAt ?? '',
  };
}

function mapParty(p: RawParty): CachedParty {
  return {
    id: p.id,
    name: p.name,
    partyType: ((p.partyType ?? p.type) ?? 2) as CachedParty['partyType'],
    rowVersion: p.rowVersion ?? null,
    updatedAt: p.updatedAt ?? '',
  };
}

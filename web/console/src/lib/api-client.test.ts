import { afterEach, describe, expect, it, vi } from 'vitest';

import { apiClient, isApiError } from '@/lib/api-client';
import { clearTokens, saveTokens } from '@/lib/token-storage';

function mockFetch(response: unknown) {
  const fn = vi.fn().mockResolvedValue(response);
  vi.stubGlobal('fetch', fn);
  return fn;
}

afterEach(() => {
  vi.unstubAllGlobals();
  clearTokens();
});

describe('apiClient', () => {
  it('başarılı GET → JSON döndürür', async () => {
    mockFetch({ ok: true, status: 200, json: async () => ({ a: 1 }) });
    await expect(apiClient.get('/x')).resolves.toEqual({ a: 1 });
  });

  it('204 No Content → undefined', async () => {
    mockFetch({
      ok: true,
      status: 204,
      json: async () => {
        throw new Error('gövde yok');
      },
    });
    await expect(apiClient.get('/x')).resolves.toBeUndefined();
  });

  it('hata ProblemDetails → ApiError (detail=mesaj, title=kod)', async () => {
    mockFetch({
      ok: false,
      status: 401,
      statusText: 'Unauthorized',
      json: async () => ({
        detail: 'Geçersiz kimlik bilgileri.',
        title: 'User.InvalidCredentials',
      }),
    });
    await expect(apiClient.get('/x')).rejects.toMatchObject({
      status: 401,
      message: 'Geçersiz kimlik bilgileri.',
      code: 'User.InvalidCredentials',
    });
  });

  it('hata gövdesi JSON değilse → statusText mesajı', async () => {
    mockFetch({
      ok: false,
      status: 500,
      statusText: 'Server Error',
      json: async () => {
        throw new Error('JSON değil');
      },
    });
    await expect(apiClient.get('/x')).rejects.toMatchObject({
      status: 500,
      message: 'Server Error',
    });
  });

  it('token varsa Authorization başlığı ekler', async () => {
    const fn = mockFetch({ ok: true, status: 200, json: async () => ({}) });
    saveTokens({ accessToken: 'abc', refreshToken: 'r' });

    await apiClient.get('/x');

    const init = fn.mock.calls[0][1] as RequestInit;
    const headers = init.headers as Headers;
    expect(headers.get('Authorization')).toBe('Bearer abc');
  });

  it('token yoksa Authorization başlığı EKLEMEZ', async () => {
    const fn = mockFetch({ ok: true, status: 200, json: async () => ({}) });

    await apiClient.get('/x');

    const init = fn.mock.calls[0][1] as RequestInit;
    const headers = init.headers as Headers;
    expect(headers.get('Authorization')).toBeNull();
  });

  it('isApiError yalnız ApiError şeklini daraltır', () => {
    expect(isApiError({ status: 1, message: 'x' })).toBe(true);
    expect(isApiError('x')).toBe(false);
    expect(isApiError(null)).toBe(false);
  });
});

'use client';

// Sayfalanmış liste uçları için genel hook. Verilen yol üreticiden (page,pageSize → path)
// çeker; sayfa durumunu, yükleme ve hata durumunu yönetir. JWT'yi apiClient ekler; tenant
// sunucuda claim'den çözülür (BK-8). Gateway üzerinden çağrılır (/api/{servis}/...).

import { useCallback, useEffect, useState } from 'react';

import { apiClient, isApiError } from '@/lib/api-client';
import { getAccessToken } from '@/lib/token-storage';
import type { PagedResult } from '@/shared/paged';

export interface PagedListState<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  loading: boolean;
  error: string | null;
  setPage: (page: number) => void;
  reload: () => void;
}

const DEFAULT_PAGE_SIZE = 20;

/**
 * @param buildPath (page, pageSize) → Gateway yolu, ör. `/api/party/parties?page=1&pageSize=20`.
 */
export function usePagedList<T>(
  buildPath: (page: number, pageSize: number) => string,
  pageSize: number = DEFAULT_PAGE_SIZE,
): PagedListState<T> {
  const [items, setItems] = useState<T[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const reload = useCallback(() => setReloadKey((k) => k + 1), []);

  useEffect(() => {
    if (!getAccessToken()) {
      setLoading(false);
      setError(null);
      setItems([]);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    apiClient
      .get<PagedResult<T>>(buildPath(page, pageSize))
      .then((result) => {
        if (cancelled) return;
        setItems(result.items ?? []);
        setTotalCount(result.totalCount ?? 0);
      })
      .catch((err) => {
        if (cancelled) return;
        setItems([]);
        setError(isApiError(err) ? err.message : 'Liste alınamadı.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [buildPath, page, pageSize, reloadKey]);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return {
    items,
    page,
    pageSize,
    totalCount,
    totalPages,
    loading,
    error,
    setPage,
    reload,
  };
}

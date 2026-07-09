'use client';

// Form seçicileri için aktif ürün listesi (id + ad + varsayılan birim). İlk sayfayı (pageSize=100)
// çeker — MVP. Gateway üzerinden, JWT+tenant sunucuda (BK-8). Satış/mal-geliş satırında ürün seçici.

import { useEffect, useState } from 'react';

import { apiClient } from '@/lib/api-client';
import { getAccessToken } from '@/lib/token-storage';
import type { Product } from '@/shared/entities';
import type { PagedResult } from '@/shared/paged';

export interface ProductOption {
  id: string;
  name: string;
  defaultUnit: number;
}

export function useProductOptions(): { options: ProductOption[]; loading: boolean } {
  const [options, setOptions] = useState<ProductOption[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!getAccessToken()) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    apiClient
      .get<PagedResult<Product>>(
        '/api/inventory/products?page=1&pageSize=100&onlyActive=true',
      )
      .then((result) => {
        if (cancelled) return;
        setOptions(
          (result.items ?? []).map((p) => ({
            id: p.id,
            name: p.name,
            defaultUnit: p.defaultUnit,
          })),
        );
      })
      .catch(() => {
        if (!cancelled) setOptions([]);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return { options, loading };
}

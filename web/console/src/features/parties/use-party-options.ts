'use client';

// Form seçicileri için taraf listesi (id + ad). İlk sayfayı (pageSize=100) çeker — MVP; taraf
// sayısı büyürse arama/sayfalı seçiciye yükseltilir. Gateway üzerinden, JWT+tenant sunucuda.

import { useEffect, useState } from 'react';

import { apiClient } from '@/lib/api-client';
import { getAccessToken } from '@/lib/token-storage';
import type { Party } from '@/shared/entities';
import type { PagedResult } from '@/shared/paged';

export interface PartyOption {
  id: string;
  displayName: string;
}

export function usePartyOptions(): { options: PartyOption[]; loading: boolean } {
  const [options, setOptions] = useState<PartyOption[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!getAccessToken()) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    apiClient
      .get<PagedResult<Party>>('/api/party/parties?page=1&pageSize=100')
      .then((result) => {
        if (cancelled) return;
        setOptions(
          (result.items ?? []).map((p) => ({
            id: p.id,
            displayName: p.displayName,
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

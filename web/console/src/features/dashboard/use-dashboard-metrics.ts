'use client';

// Dashboard özet metriklerini (okuma-modeli) API Gateway üzerinden çeken hook. Her metrik
// bağımsız yükleme/hata durumu taşır — biri başarısız olsa diğerleri görünür. Yalnız oturum
// açık (token var) iken çeker; JWT'yi apiClient ekler, tenant sunucuda claim'den çözülür (BK-8).

import { useEffect, useState } from 'react';

import { apiClient, isApiError } from '@/lib/api-client';
import { getAccessToken } from '@/lib/token-storage';
import type { AgingReport, DailySalesSummary, StockItem } from './types';

export interface Metric<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

export interface DashboardMetrics {
  daily: Metric<DailySalesSummary>;
  aging: Metric<AgingReport>;
  lowStock: Metric<StockItem[]>;
}

const LOADING = { data: null, loading: true, error: null } as const;

/** Bugünün tarihini backend'in beklediği tarih (yyyy-MM-dd) olarak döndürür. */
function today(): string {
  return new Date().toISOString().slice(0, 10);
}

function toError(err: unknown): string {
  if (isApiError(err)) return err.message;
  return 'Veri alınamadı.';
}

export function useDashboardMetrics(): DashboardMetrics {
  const [daily, setDaily] = useState<Metric<DailySalesSummary>>(LOADING);
  const [aging, setAging] = useState<Metric<AgingReport>>(LOADING);
  const [lowStock, setLowStock] = useState<Metric<StockItem[]>>(LOADING);

  useEffect(() => {
    if (!getAccessToken()) {
      const unauth = { data: null, loading: false, error: null };
      setDaily(unauth);
      setAging(unauth);
      setLowStock(unauth);
      return;
    }

    let cancelled = false;

    // Her metriği bağımsız çek; birinin hatası diğerini etkilemez (Promise zincirleri ayrı).
    function load<T>(
      path: string,
      set: (m: Metric<T>) => void,
    ): Promise<void> {
      return apiClient
        .get<T>(path)
        .then((data) => {
          if (!cancelled) set({ data, loading: false, error: null });
        })
        .catch((err) => {
          if (!cancelled) set({ data: null, loading: false, error: toError(err) });
        });
    }

    void load<DailySalesSummary>(
      `/api/sales/reports/daily?day=${today()}`,
      setDaily,
    );
    void load<AgingReport>(`/api/finance/reports/aging`, setAging);
    void load<StockItem[]>(`/api/inventory/stock/low-stock`, setLowStock);

    return () => {
      cancelled = true;
    };
  }, []);

  return { daily, aging, lowStock };
}

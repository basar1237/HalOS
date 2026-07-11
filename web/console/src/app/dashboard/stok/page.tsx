'use client';

// Stok & Depo — sayfalanmış stok listesi (GET /api/inventory/stock). Kalan miktar Σ hareket
// ile türetilir (backend); eşik altı kalemler işaretlenir.

import Link from 'next/link';
import { useCallback, useEffect, useMemo, useState } from 'react';

import { PagedTable, type Column } from '@/components/paged-table';
import { shortId } from '@/features/common/labels';
import { usePagedList } from '@/features/common/use-paged-list';
import { useProductOptions } from '@/features/products/use-product-options';
import { apiClient } from '@/lib/api-client';
import { getAccessToken } from '@/lib/token-storage';
import type { StockItem } from '@/shared/entities';

const QTY = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 3 });

function isLow(s: StockItem): boolean {
  return s.reorderThreshold != null && s.quantityOnHand <= s.reorderThreshold;
}

/** Depo id→ad haritası (Inventory servisinden; stok yalnız ID tutar). */
function useWarehouseNames(): (id: string) => string {
  const [map, setMap] = useState<Map<string, string>>(new Map());
  useEffect(() => {
    if (!getAccessToken()) return;
    let cancelled = false;
    apiClient
      .get<{ id: string; name: string }[]>('/api/inventory/warehouses')
      .then((rows) => {
        if (cancelled) return;
        const m = new Map<string, string>();
        for (const w of rows ?? []) m.set(w.id, w.name);
        setMap(m);
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, []);
  return (id: string) => map.get(id) ?? shortId(id);
}

export default function StockPage() {
  const buildPath = useCallback(
    (page: number, pageSize: number) =>
      `/api/inventory/stock?page=${page}&pageSize=${pageSize}`,
    [],
  );
  const state = usePagedList<StockItem>(buildPath);

  const { options: products } = useProductOptions();
  const productName = useMemo(() => {
    const m = new Map<string, string>();
    for (const p of products) m.set(p.id, p.name);
    return (id: string) => m.get(id) ?? shortId(id);
  }, [products]);
  const warehouseName = useWarehouseNames();

  const COLUMNS: Column<StockItem>[] = [
    { header: 'Ürün', cell: (s) => productName(s.productId) },
    { header: 'Depo', cell: (s) => warehouseName(s.warehouseId) },
    { header: 'Kalan', align: 'num', cell: (s) => QTY.format(s.quantityOnHand) },
    {
      header: 'Eşik',
      align: 'num',
      cell: (s) => (s.reorderThreshold != null ? QTY.format(s.reorderThreshold) : '—'),
    },
    {
      header: 'Durum',
      cell: (s) =>
        isLow(s) ? <span className="badge badge--warn">Eşik altı</span> : 'Normal',
    },
  ];

  return (
    <div>
      <div className="page-head">
        <h1 className="page-title">Stok & Depo</h1>
        <div className="btn-group">
          <Link href="/dashboard/stok/fire" className="btn-secondary btn-sm">
            Fire Kaydet
          </Link>
          <Link href="/dashboard/stok/urunler" className="btn-secondary btn-sm">
            Ürün Kataloğu
          </Link>
        </div>
      </div>
      <PagedTable
        state={state}
        columns={COLUMNS}
        rowKey={(s) => s.id}
        emptyText="Stok kaydı yok."
      />
    </div>
  );
}

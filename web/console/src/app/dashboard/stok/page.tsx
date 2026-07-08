'use client';

// Stok & Depo — sayfalanmış stok listesi (GET /api/inventory/stock). Kalan miktar Σ hareket
// ile türetilir (backend); eşik altı kalemler işaretlenir.

import { useCallback } from 'react';

import { PagedTable, type Column } from '@/components/paged-table';
import { shortId } from '@/features/common/labels';
import { usePagedList } from '@/features/common/use-paged-list';
import type { StockItem } from '@/shared/entities';

const QTY = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 3 });

function isLow(s: StockItem): boolean {
  return s.reorderThreshold != null && s.quantityOnHand <= s.reorderThreshold;
}

const COLUMNS: Column<StockItem>[] = [
  { header: 'Ürün', cell: (s) => shortId(s.productId) },
  { header: 'Depo', cell: (s) => shortId(s.warehouseId) },
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

export default function StockPage() {
  const buildPath = useCallback(
    (page: number, pageSize: number) =>
      `/api/inventory/stock?page=${page}&pageSize=${pageSize}`,
    [],
  );
  const state = usePagedList<StockItem>(buildPath);

  return (
    <div>
      <h1 className="page-title">Stok & Depo</h1>
      <PagedTable
        state={state}
        columns={COLUMNS}
        rowKey={(s) => s.id}
        emptyText="Stok kaydı yok."
      />
    </div>
  );
}

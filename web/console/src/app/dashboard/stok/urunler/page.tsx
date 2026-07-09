'use client';

// Ürün Kataloğu yönetimi — sayfalanmış liste (GET /api/inventory/products, onlyActive=false → tümü).
// Satış/mal-geliş satırlarındaki ürün seçici bu katalogtan beslenir (docs/03 M2).

import Link from 'next/link';
import { useCallback } from 'react';

import { PagedTable, type Column } from '@/components/paged-table';
import { usePagedList } from '@/features/common/use-paged-list';
import { label } from '@/features/common/labels';
import { UNIT_LABEL } from '@/features/sales/sales-api';
import type { Product } from '@/shared/entities';

const COLUMNS: Column<Product>[] = [
  { header: 'Ad', cell: (p) => p.name },
  { header: 'Kategori', cell: (p) => p.category ?? '—' },
  { header: 'Varsayılan Birim', cell: (p) => label(UNIT_LABEL, p.defaultUnit) },
  {
    header: 'Durum',
    cell: (p) =>
      p.isActive ? (
        <span className="badge badge--ok">Aktif</span>
      ) : (
        <span className="badge">Pasif</span>
      ),
  },
];

export default function ProductsPage() {
  const buildPath = useCallback(
    (page: number, pageSize: number) =>
      `/api/inventory/products?page=${page}&pageSize=${pageSize}&onlyActive=false`,
    [],
  );
  const state = usePagedList<Product>(buildPath);

  return (
    <div>
      <div className="page-head">
        <h1 className="page-title">Ürün Kataloğu</h1>
        <div className="btn-group">
          <Link href="/dashboard/stok" className="btn-secondary btn-sm">
            Stok
          </Link>
          <Link
            href="/dashboard/stok/urunler/yeni"
            className="btn-primary btn-inline"
          >
            Yeni Ürün
          </Link>
        </div>
      </div>
      <PagedTable
        state={state}
        columns={COLUMNS}
        rowKey={(p) => p.id}
        emptyText="Kayıtlı ürün yok."
      />
    </div>
  );
}

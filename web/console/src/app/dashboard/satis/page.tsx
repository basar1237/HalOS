'use client';

// Satış & Komisyon — sayfalanmış satış listesi (GET /api/sales/sales, sold_at azalan).
// "Yeni Satış" formu satışı uçtan uca oluşturup tamamlar (kesinti/hakediş motoru).

import Link from 'next/link';
import { useCallback } from 'react';

import { PagedTable, type Column } from '@/components/paged-table';
import { label, shortId } from '@/features/common/labels';
import { SALE_STATUS_LABEL } from '@/features/sales/sales-api';
import { usePagedList } from '@/features/common/use-paged-list';
import type { Sale } from '@/shared/entities';

const TRY = new Intl.NumberFormat('tr-TR', {
  style: 'currency',
  currency: 'TRY',
});

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? '—' : d.toLocaleDateString('tr-TR');
}

function statusBadge(status: number): string {
  if (status === 2) return 'badge badge--ok'; // Tamamlandı
  if (status === 3) return 'badge badge--error'; // İptal
  return 'badge'; // Taslak
}

const COLUMNS: Column<Sale>[] = [
  { header: 'Tarih', cell: (s) => formatDate(s.soldAt) },
  { header: 'Alıcı', cell: (s) => shortId(s.buyerPartyId) },
  { header: 'Müstahsil', cell: (s) => shortId(s.producerPartyId) },
  { header: 'Tutar', align: 'num', cell: (s) => TRY.format(s.grossAmount) },
  {
    header: 'Durum',
    cell: (s) => (
      <span className={statusBadge(s.status)}>
        {label(SALE_STATUS_LABEL, s.status)}
      </span>
    ),
  },
];

export default function SalesPage() {
  const buildPath = useCallback(
    (page: number, pageSize: number) =>
      `/api/sales/sales?page=${page}&pageSize=${pageSize}`,
    [],
  );
  const state = usePagedList<Sale>(buildPath);

  return (
    <div>
      <div className="page-head">
        <h1 className="page-title">Satış & Komisyon</h1>
        <Link href="/dashboard/satis/yeni" className="btn-primary btn-inline">
          Yeni Satış
        </Link>
      </div>
      <PagedTable
        state={state}
        columns={COLUMNS}
        rowKey={(s) => s.id}
        emptyText="Satış kaydı yok."
      />
    </div>
  );
}

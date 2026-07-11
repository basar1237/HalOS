'use client';

// Satış & Komisyon — sayfalanmış satış listesi (GET /api/sales/sales, sold_at azalan).
// "Yeni Satış" formu satışı uçtan uca oluşturup tamamlar (kesinti/hakediş motoru).

import Link from 'next/link';
import { useCallback, useMemo } from 'react';

import { PagedTable, type Column } from '@/components/paged-table';
import { label, shortId } from '@/features/common/labels';
import { SALE_STATUS_LABEL } from '@/features/sales/sales-api';
import { usePagedList } from '@/features/common/use-paged-list';
import { usePartyOptions } from '@/features/parties/use-party-options';
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

export default function SalesPage() {
  const buildPath = useCallback(
    (page: number, pageSize: number) =>
      `/api/sales/sales?page=${page}&pageSize=${pageSize}`,
    [],
  );
  const state = usePagedList<Sale>(buildPath);

  // Taraf ID'lerini isimlere çevirmek için (mikroservis: Satış yalnız ID tutar, isim Party'de).
  const { options: parties } = usePartyOptions();
  const partyName = useMemo(() => {
    const m = new Map<string, string>();
    for (const p of parties) m.set(p.id, p.displayName);
    return (id: string) => m.get(id) ?? shortId(id);
  }, [parties]);

  const COLUMNS: Column<Sale>[] = [
    { header: 'Tarih', cell: (s) => formatDate(s.soldAt) },
    { header: 'Alıcı', cell: (s) => partyName(s.buyerPartyId) },
    { header: 'Müstahsil', cell: (s) => partyName(s.producerPartyId) },
    { header: 'Tutar', align: 'num', cell: (s) => TRY.format(s.grossAmount) },
    {
      header: 'Durum',
      cell: (s) => (
        <span className={statusBadge(s.status)}>
          {label(SALE_STATUS_LABEL, s.status)}
        </span>
      ),
    },
    {
      header: '',
      cell: (s) => (
        <Link href={`/dashboard/satis/${s.id}`} className="row-link">
          Detay
        </Link>
      ),
    },
  ];

  return (
    <div>
      <div className="page-head">
        <h1 className="page-title">Satış & Komisyon</h1>
        <div className="btn-group">
          <Link
            href="/dashboard/satis/mal-gelis"
            className="btn-secondary btn-sm"
          >
            Yeni Mal Geliş
          </Link>
          <Link href="/dashboard/satis/yeni" className="btn-primary btn-inline">
            Yeni Satış
          </Link>
        </div>
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

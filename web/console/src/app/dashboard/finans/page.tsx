'use client';

// Cari & Finans — sayfalanmış cari hesap listesi (GET /api/finance/current-accounts).
// Bakiye Σ hareket ile türetilir (backend); pozitif=alacak, negatif=borç gösterimi.

import Link from 'next/link';
import { useCallback } from 'react';

import { PagedTable, type Column } from '@/components/paged-table';
import { shortId } from '@/features/common/labels';
import { usePagedList } from '@/features/common/use-paged-list';
import type { CurrentAccount } from '@/shared/entities';

const TRY = new Intl.NumberFormat('tr-TR', {
  style: 'currency',
  currency: 'TRY',
});

const COLUMNS: Column<CurrentAccount>[] = [
  { header: 'Taraf', cell: (a) => shortId(a.partyId) },
  { header: 'Bakiye', align: 'num', cell: (a) => TRY.format(a.balance) },
  { header: 'Hareket', align: 'num', cell: (a) => a.entryCount },
];

export default function FinancePage() {
  const buildPath = useCallback(
    (page: number, pageSize: number) =>
      `/api/finance/current-accounts?page=${page}&pageSize=${pageSize}`,
    [],
  );
  const state = usePagedList<CurrentAccount>(buildPath);

  return (
    <div>
      <div className="page-head">
        <h1 className="page-title">Cari & Finans</h1>
        <div className="btn-group">
          <Link
            href="/dashboard/finans/hareket?tur=payment"
            className="btn-secondary btn-sm"
          >
            Ödeme
          </Link>
          <Link
            href="/dashboard/finans/hareket?tur=collection"
            className="btn-secondary btn-sm"
          >
            Tahsilat
          </Link>
          <Link
            href="/dashboard/finans/hareket?tur=advance"
            className="btn-secondary btn-sm"
          >
            Avans
          </Link>
        </div>
      </div>
      <PagedTable
        state={state}
        columns={COLUMNS}
        rowKey={(a) => a.id}
        emptyText="Cari hesap yok."
      />
    </div>
  );
}

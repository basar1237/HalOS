'use client';

// e-Belge & HKS — e-Fatura listesi (GET /api/integration/invoices). e-MM/HKS/künye sekmeleri
// sonraki adımda; bu sayfa faturaları listeler (durum/senaryo/tür etiketli).

import { useCallback } from 'react';

import { PagedTable, type Column } from '@/components/paged-table';
import {
  INVOICE_SCENARIO_LABEL,
  INVOICE_STATUS_LABEL,
  INVOICE_TYPE_LABEL,
  label,
} from '@/features/common/labels';
import { usePagedList } from '@/features/common/use-paged-list';
import type { Invoice } from '@/shared/entities';

const TRY = new Intl.NumberFormat('tr-TR', {
  style: 'currency',
  currency: 'TRY',
});

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? '—' : d.toLocaleDateString('tr-TR');
}

/** Fatura durumuna göre rozet sınıfı — başarısız kırmızı, düzenlendi yeşil, diğer nötr. */
function statusBadge(status: number): string {
  if (status === 3) return 'badge badge--error'; // Başarısız
  if (status === 2) return 'badge badge--ok'; // Düzenlendi
  return 'badge';
}

const COLUMNS: Column<Invoice>[] = [
  { header: 'Belge No', cell: (i) => i.invoiceNumber ?? '—' },
  { header: 'Tarih', cell: (i) => formatDate(i.issueDate) },
  { header: 'Senaryo', cell: (i) => label(INVOICE_SCENARIO_LABEL, i.scenario) },
  { header: 'Tür', cell: (i) => label(INVOICE_TYPE_LABEL, i.type) },
  { header: 'Tutar', align: 'num', cell: (i) => TRY.format(i.totalAmount) },
  {
    header: 'Durum',
    cell: (i) => (
      <span className={statusBadge(i.status)}>
        {label(INVOICE_STATUS_LABEL, i.status)}
      </span>
    ),
  },
];

export default function EDocumentsPage() {
  const buildPath = useCallback(
    (page: number, pageSize: number) =>
      `/api/integration/invoices?page=${page}&pageSize=${pageSize}`,
    [],
  );
  const state = usePagedList<Invoice>(buildPath);

  return (
    <div>
      <h1 className="page-title">e-Belge & HKS</h1>
      <p className="page-lead">e-Fatura kayıtları</p>
      <PagedTable
        state={state}
        columns={COLUMNS}
        rowKey={(i) => i.id}
        emptyText="e-Fatura kaydı yok."
      />
    </div>
  );
}

'use client';

// e-Belge & HKS — sekmeli okuma: e-Fatura / e-MM (müstahsil makbuzu) / HKS bildirimi. Üçü de
// Integration servisinin sayfalı uçlarından gelir (docs/03 M7/M8). Belgeler satış tamamlanınca
// otomatik üretilir; bu ekran durum/numara izler (GİB/HKS gateway şu an STUB — Faz 1 borcu).

import { useCallback, useState } from 'react';

import { PagedTable, type Column } from '@/components/paged-table';
import {
  HKS_STATUS_LABEL,
  INVOICE_STATUS_LABEL,
  label,
  RECEIPT_STATUS_LABEL,
  shortId,
} from '@/features/common/labels';
import { usePagedList } from '@/features/common/use-paged-list';
import type { HksNotification, Invoice, ProducerReceipt } from '@/shared/entities';

const TRY = new Intl.NumberFormat('tr-TR', {
  style: 'currency',
  currency: 'TRY',
});

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? '—' : d.toLocaleDateString('tr-TR');
}

// Ortak durum rozeti: 2=başarılı (yeşil), 3=başarısız (kırmızı), diğer nötr.
function statusBadge(status: number): string {
  if (status === 3) return 'badge badge--error';
  if (status === 2) return 'badge badge--ok';
  return 'badge';
}

type Tab = 'invoices' | 'receipts' | 'hks';

const INVOICE_COLUMNS: Column<Invoice>[] = [
  { header: 'Belge No', cell: (i) => i.invoiceNumber ?? '—' },
  { header: 'Tarih', cell: (i) => formatDate(i.issueDate) },
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

const RECEIPT_COLUMNS: Column<ProducerReceipt>[] = [
  { header: 'Makbuz No', cell: (r) => r.receiptNumber ?? '—' },
  { header: 'Tarih', cell: (r) => formatDate(r.issueDate) },
  { header: 'Müstahsil', cell: (r) => shortId(r.producerPartyId) },
  { header: 'Brüt', align: 'num', cell: (r) => TRY.format(r.grossAmount) },
  { header: 'Net Ödenecek', align: 'num', cell: (r) => TRY.format(r.netPayable) },
  {
    header: 'Durum',
    cell: (r) => (
      <span className={statusBadge(r.status)}>
        {label(RECEIPT_STATUS_LABEL, r.status)}
      </span>
    ),
  },
];

const HKS_COLUMNS: Column<HksNotification>[] = [
  { header: 'Referans No', cell: (h) => h.referenceNumber ?? '—' },
  { header: 'Tarih', cell: (h) => formatDate(h.notifiedDate) },
  { header: 'Brüt', align: 'num', cell: (h) => TRY.format(h.grossAmount) },
  { header: 'Rüsum', align: 'num', cell: (h) => TRY.format(h.marketFeeAmount) },
  {
    header: 'Durum',
    cell: (h) => (
      <span className={statusBadge(h.status)}>
        {label(HKS_STATUS_LABEL, h.status)}
      </span>
    ),
  },
];

export default function EDocumentsPage() {
  const [tab, setTab] = useState<Tab>('invoices');

  const invoicesPath = useCallback(
    (page: number, size: number) =>
      `/api/integration/invoices?page=${page}&pageSize=${size}`,
    [],
  );
  const receiptsPath = useCallback(
    (page: number, size: number) =>
      `/api/integration/producer-receipts?page=${page}&pageSize=${size}`,
    [],
  );
  const hksPath = useCallback(
    (page: number, size: number) =>
      `/api/integration/hks-notifications?page=${page}&pageSize=${size}`,
    [],
  );

  const invoices = usePagedList<Invoice>(invoicesPath);
  const receipts = usePagedList<ProducerReceipt>(receiptsPath);
  const hks = usePagedList<HksNotification>(hksPath);

  return (
    <div>
      <h1 className="page-title">e-Belge & HKS</h1>

      <div className="tabs">
        <button
          type="button"
          className={tab === 'invoices' ? 'tab tab--active' : 'tab'}
          onClick={() => setTab('invoices')}
        >
          e-Fatura
        </button>
        <button
          type="button"
          className={tab === 'receipts' ? 'tab tab--active' : 'tab'}
          onClick={() => setTab('receipts')}
        >
          e-Müstahsil Makbuzu
        </button>
        <button
          type="button"
          className={tab === 'hks' ? 'tab tab--active' : 'tab'}
          onClick={() => setTab('hks')}
        >
          HKS Bildirimi
        </button>
      </div>

      {tab === 'invoices' && (
        <PagedTable
          state={invoices}
          columns={INVOICE_COLUMNS}
          rowKey={(i) => i.id}
          emptyText="e-Fatura kaydı yok."
        />
      )}
      {tab === 'receipts' && (
        <PagedTable
          state={receipts}
          columns={RECEIPT_COLUMNS}
          rowKey={(r) => r.id}
          emptyText="e-Müstahsil makbuzu yok."
        />
      )}
      {tab === 'hks' && (
        <PagedTable
          state={hks}
          columns={HKS_COLUMNS}
          rowKey={(h) => h.id}
          emptyText="HKS bildirimi yok."
        />
      )}
    </div>
  );
}

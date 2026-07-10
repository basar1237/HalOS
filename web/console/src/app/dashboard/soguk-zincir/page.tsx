'use client';

// Soğuk Zincir & IoT — soğuk oda listesi (GET /api/coldchain/cold-storage-units). Son sıcaklık
// eşik dışıysa ALARM işaretlenir (docs/04 §6, S3.1). Canlı eşik-aşımı bildirimleri ayrıca
// Bildirimler ekranına düşer (Notification → TemperatureThresholdBreached).

import Link from 'next/link';
import { useCallback } from 'react';

import { PagedTable, type Column } from '@/components/paged-table';
import { type ColdStorageUnit, isBreaching } from '@/features/coldchain/coldchain-api';
import { usePagedList } from '@/features/common/use-paged-list';

const TEMP = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 1 });

function formatTemp(value: number | null): string {
  return value == null ? '—' : `${TEMP.format(value)} °C`;
}

const COLUMNS: Column<ColdStorageUnit>[] = [
  {
    header: 'Soğuk Oda',
    cell: (u) => (
      <Link href={`/dashboard/soguk-zincir/${u.id}`} className="row-link">
        {u.name}
      </Link>
    ),
  },
  {
    header: 'İzin Verilen Aralık',
    cell: (u) => `${TEMP.format(u.minTempC)} … ${TEMP.format(u.maxTempC)} °C`,
  },
  { header: 'Son Sıcaklık', align: 'num', cell: (u) => formatTemp(u.latestTemperatureC) },
  {
    header: 'Durum',
    cell: (u) => {
      if (!u.isActive) return <span className="badge">Pasif</span>;
      if (u.latestTemperatureC == null) return <span className="badge">Veri yok</span>;
      return isBreaching(u) ? (
        <span className="badge badge--warn">Alarm</span>
      ) : (
        'Normal'
      );
    },
  },
];

export default function ColdChainPage() {
  const buildPath = useCallback(
    (page: number, pageSize: number) =>
      `/api/coldchain/cold-storage-units?page=${page}&pageSize=${pageSize}`,
    [],
  );
  const state = usePagedList<ColdStorageUnit>(buildPath);

  return (
    <div>
      <div className="page-head">
        <h1 className="page-title">Soğuk Zincir & IoT</h1>
        <div className="btn-group">
          <Link href="/dashboard/soguk-zincir/yeni" className="btn-secondary btn-sm">
            Yeni Soğuk Oda
          </Link>
        </div>
      </div>
      <PagedTable
        state={state}
        columns={COLUMNS}
        rowKey={(u) => u.id}
        emptyText="Tanımlı soğuk oda yok."
      />
    </div>
  );
}

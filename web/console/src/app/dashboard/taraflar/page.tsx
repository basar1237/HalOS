'use client';

// Taraflar (müstahsil/alıcı/tüccar/konsinyeci) — sayfalanmış liste (GET /api/party/parties).

import { useCallback } from 'react';

import { PagedTable, type Column } from '@/components/paged-table';
import { label, PARTY_ROLE_LABEL } from '@/features/common/labels';
import { usePagedList } from '@/features/common/use-paged-list';
import type { Party } from '@/shared/entities';

const COLUMNS: Column<Party>[] = [
  { header: 'Ad', cell: (p) => p.displayName },
  {
    header: 'Roller',
    cell: (p) => p.roles.map((r) => label(PARTY_ROLE_LABEL, r)).join(', ') || '—',
  },
  { header: 'VKN/TCKN', cell: (p) => p.vkn ?? p.tckn ?? '—' },
  { header: 'Telefon', cell: (p) => p.phone ?? '—' },
  {
    header: 'Kayıt Tutan',
    cell: (p) => (p.keepsRecords ? 'Evet' : 'Hayır'),
  },
  { header: 'Durum', cell: (p) => (p.isActive ? 'Aktif' : 'Pasif') },
];

export default function PartiesPage() {
  const buildPath = useCallback(
    (page: number, pageSize: number) =>
      `/api/party/parties?page=${page}&pageSize=${pageSize}`,
    [],
  );
  const state = usePagedList<Party>(buildPath);

  return (
    <div>
      <h1 className="page-title">Taraflar</h1>
      <PagedTable
        state={state}
        columns={COLUMNS}
        rowKey={(p) => p.id}
        emptyText="Kayıtlı taraf yok."
      />
    </div>
  );
}

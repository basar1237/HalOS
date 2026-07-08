'use client';

// Sayfalanmış liste tablosu — yükleme/hata/boş/satırlar + sayfalama kontrolü. Liste sayfaları
// yalnız sütunları ve veri kaynağını (usePagedList state) verir; bu bileşen gerisini yapar.

import type { ReactNode } from 'react';

import type { PagedListState } from '@/features/common/use-paged-list';

export interface Column<T> {
  header: string;
  cell: (row: T) => ReactNode;
  /** Sağa hizalı sayısal sütunlar için 'num'. */
  align?: 'num';
}

interface PagedTableProps<T> {
  state: PagedListState<T>;
  columns: Column<T>[];
  rowKey: (row: T) => string;
  emptyText?: string;
}

export function PagedTable<T>({
  state,
  columns,
  rowKey,
  emptyText = 'Kayıt yok.',
}: PagedTableProps<T>) {
  const { items, loading, error, page, totalPages, totalCount, setPage } = state;

  if (loading) {
    return <p className="page-state">Yükleniyor…</p>;
  }
  if (error) {
    return <p className="page-state page-state--error">{error}</p>;
  }
  if (items.length === 0) {
    return <p className="page-state">{emptyText}</p>;
  }

  return (
    <div>
      <table className="data-table">
        <thead>
          <tr>
            {columns.map((col) => (
              <th
                key={col.header}
                className={col.align === 'num' ? 'data-table__num' : undefined}
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {items.map((row) => (
            <tr key={rowKey(row)}>
              {columns.map((col) => (
                <td
                  key={col.header}
                  className={col.align === 'num' ? 'data-table__num' : undefined}
                >
                  {col.cell(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>

      <div className="pager">
        <button
          type="button"
          className="pager__btn"
          disabled={page <= 1}
          onClick={() => setPage(page - 1)}
        >
          Önceki
        </button>
        <span className="pager__info">
          Sayfa {page} / {totalPages} · {totalCount} kayıt
        </span>
        <button
          type="button"
          className="pager__btn"
          disabled={page >= totalPages}
          onClick={() => setPage(page + 1)}
        >
          Sonraki
        </button>
      </div>
    </div>
  );
}

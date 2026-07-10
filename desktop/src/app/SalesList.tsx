import { formatDateTimeTR, formatTRY } from '../lib/format';
import type { SaleListItem } from '../lib/db';

const SYNC_LABELS: Record<string, string> = {
  pending: 'Bekliyor',
  synced: 'Gönderildi',
  conflict: 'Çakışma',
};

const TERM_LABELS: Record<number, string> = { 1: 'Peşin', 2: 'Vadeli' };

interface Props {
  sales: SaleListItem[];
}

export function SalesList({ sales }: Props) {
  return (
    <section className="panel">
      <h2>Satışlar ({sales.length})</h2>
      {sales.length === 0 ? (
        <p className="muted">Henüz satış yok.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Tarih</th>
              <th>Alıcı</th>
              <th>Vade</th>
              <th className="num">Tutar</th>
              <th>Durum</th>
            </tr>
          </thead>
          <tbody>
            {sales.map((s) => (
              <tr key={s.operationId}>
                <td>{formatDateTimeTR(s.createdAt)}</td>
                <td>{s.partyName}</td>
                <td>{TERM_LABELS[s.saleTerm] ?? s.saleTerm}</td>
                <td className="num">{formatTRY(s.grossTotal)}</td>
                <td>
                  <span className={`tag ${s.syncStatus}`}>
                    {SYNC_LABELS[s.syncStatus] ?? s.syncStatus}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

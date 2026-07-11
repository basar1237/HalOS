import { useEffect, useState } from 'react';
import { apiGet } from '../../lib/api';
import { formatTRY } from '../../lib/format';
import { useLookups } from '../../lib/lookups';

interface Account { id: string; partyId: string; balance: number; entryCount: number }
interface Paged<T> { items?: T[]; totalCount?: number }

export function Cari() {
  const { partyName } = useLookups();
  const [rows, setRows] = useState<Account[]>([]);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    apiGet<Paged<Account>>('/api/finance/current-accounts?page=1&pageSize=200')
      .then((r) => setRows(r.items ?? []))
      .catch(() => setErr('Cari hesaplar alınamadı.'));
  }, []);

  return (
    <section className="panel">
      <h2>Cari &amp; Finans ({rows.length})</h2>
      {err && <p className="error">{err}</p>}
      {rows.length === 0 ? (
        <p className="muted">Cari hesap yok.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Taraf</th>
              <th className="num">Bakiye</th>
              <th className="num">Hareket</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((a) => (
              <tr key={a.id}>
                <td>{partyName(a.partyId)}</td>
                <td className="num" style={{ color: a.balance < 0 ? 'var(--danger)' : 'inherit' }}>{formatTRY(a.balance)}</td>
                <td className="num">{a.entryCount}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

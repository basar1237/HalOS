import { formatTRY } from '../../lib/format';
import { useLookups } from '../../lib/lookups';
import { useFetch } from '../../lib/useFetch';

interface Account { id: string; partyId: string; balance: number; entryCount: number }
interface Paged<T> { items?: T[] }

export function Cari() {
  const { partyName } = useLookups();
  const { data, error } = useFetch<Paged<Account>>('/api/finance/current-accounts?page=1&pageSize=200');
  const rows = data?.items ?? [];

  return (
    <section className="panel">
      <h2>Cari &amp; Finans ({rows.length})</h2>
      {error && <p className="error">{error}</p>}
      {rows.length === 0 ? (
        <p className="muted">Cari hesap yok.</p>
      ) : (
        <table>
          <thead>
            <tr><th>Taraf</th><th className="num">Bakiye</th><th className="num">Hareket</th></tr>
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

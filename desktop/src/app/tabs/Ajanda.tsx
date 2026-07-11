import { useEffect, useMemo, useState } from 'react';
import { apiGet } from '../../lib/api';
import { formatTRY } from '../../lib/format';

interface Cheque { id: string; kind: number; bankName: string; amount: number; dueDate: string; status: number }
interface Paged<T> { items?: T[] }
interface AgingBucket { amount: number; accountCount: number }
interface Aging { current: AgingBucket; days0To15?: AgingBucket; days16To30?: AgingBucket; over30?: AgingBucket }

const KIND: Record<number, string> = { 1: 'Çek', 2: 'Senet' };
// Tahsil edilmemiş (açık) durumlar: portföyde / tahsile verildi.
const OPEN = new Set([1, 2]);

function dayDiff(iso: string): number {
  const due = new Date(iso); due.setHours(0, 0, 0, 0);
  const today = new Date(); today.setHours(0, 0, 0, 0);
  return Math.round((due.getTime() - today.getTime()) / 86400000);
}
function d(iso: string) { const x = new Date(iso); return isNaN(x.getTime()) ? '—' : x.toLocaleDateString('tr-TR'); }

export function Ajanda() {
  const [cheques, setCheques] = useState<Cheque[]>([]);
  const [aging, setAging] = useState<Aging | null>(null);

  useEffect(() => {
    apiGet<Paged<Cheque>>('/api/finance/cheques?page=1&pageSize=200').then((r) => setCheques(r.items ?? [])).catch(() => {});
    apiGet<Aging>('/api/finance/reports/aging').then(setAging).catch(() => {});
  }, []);

  const groups = useMemo(() => {
    const open = cheques.filter((c) => OPEN.has(c.status)).map((c) => ({ ...c, diff: dayDiff(c.dueDate) }));
    return {
      overdue: open.filter((c) => c.diff < 0).sort((a, b) => a.diff - b.diff),
      today: open.filter((c) => c.diff === 0),
      week: open.filter((c) => c.diff > 0 && c.diff <= 7).sort((a, b) => a.diff - b.diff),
      later: open.filter((c) => c.diff > 7).sort((a, b) => a.diff - b.diff),
    };
  }, [cheques]);

  const overdueReceivable = (aging?.days0To15?.amount ?? 0) + (aging?.days16To30?.amount ?? 0) + (aging?.over30?.amount ?? 0);

  return (
    <section className="panel">
      <h2>Akıllı Ajanda — Hatırlatmalar</h2>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px,1fr))', gap: 14, margin: '8px 0 20px' }}>
        <Kpi label="Vadesi geçmiş çek/senet" value={String(groups.overdue.length)} danger={groups.overdue.length > 0} />
        <Kpi label="Bugün vadeli" value={String(groups.today.length)} />
        <Kpi label="7 gün içinde" value={String(groups.week.length)} />
        <Kpi label="Geciken alacak (cari)" value={formatTRY(overdueReceivable)} danger={overdueReceivable > 0} />
      </div>

      <Bucket title="⚠️ Vadesi Geçmiş" items={groups.overdue} tag="conflict" />
      <Bucket title="📅 Bugün" items={groups.today} tag="pending" />
      <Bucket title="🔜 Bu Hafta (7 gün)" items={groups.week} tag="pending" />
      <Bucket title="İleride" items={groups.later} tag="" />

      {cheques.filter((c) => OPEN.has(c.status)).length === 0 && (
        <p className="muted">Açık (tahsil edilmemiş) çek/senet yok.</p>
      )}
    </section>
  );
}

function Kpi({ label, value, danger }: { label: string; value: string; danger?: boolean }) {
  return (
    <div style={{ border: `1px solid ${danger ? '#fecaca' : 'var(--line)'}`, borderRadius: 10, padding: 16, background: danger ? '#fef2f2' : 'var(--panel-2)' }}>
      <div style={{ fontSize: 12, color: 'var(--muted)' }}>{label}</div>
      <div style={{ fontSize: 22, fontWeight: 700, color: danger ? 'var(--danger)' : 'inherit' }}>{value}</div>
    </div>
  );
}

function Bucket({ title, items, tag }: { title: string; items: (Cheque & { diff: number })[]; tag: string }) {
  if (items.length === 0) return null;
  return (
    <div style={{ marginBottom: 18 }}>
      <h2 style={{ fontSize: 14, margin: '0 0 8px' }}>{title} ({items.length})</h2>
      <table>
        <thead><tr><th>Tür</th><th>Banka</th><th className="num">Tutar</th><th>Vade</th><th>Kalan</th></tr></thead>
        <tbody>
          {items.map((c) => (
            <tr key={c.id}>
              <td>{KIND[c.kind]}</td>
              <td>{c.bankName || '—'}</td>
              <td className="num">{formatTRY(c.amount)}</td>
              <td>{d(c.dueDate)}</td>
              <td><span className={`tag ${tag}`}>{c.diff < 0 ? `${-c.diff} gün geçti` : c.diff === 0 ? 'bugün' : `${c.diff} gün`}</span></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

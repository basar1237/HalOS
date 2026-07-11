import { useEffect, useState } from 'react';
import { apiGet } from '../../lib/api';
import { formatTRY } from '../../lib/format';

interface Daily { count: number; gross: number; commission: number; net: number }
interface Dash { todayConsignmentCount: number; pendingSettlementTotal: number }
interface AgingBucket { amount: number; accountCount: number }
interface Aging { current: AgingBucket }
interface Pending { pendingInvoices: number; pendingProducerReceipts: number; total: number }

function todayStr(): string {
  return new Date().toISOString().slice(0, 10);
}

export function Dashboard() {
  const [daily, setDaily] = useState<Daily | null>(null);
  const [dash, setDash] = useState<Dash | null>(null);
  const [aging, setAging] = useState<Aging | null>(null);
  const [pending, setPending] = useState<Pending | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    const t = todayStr();
    Promise.allSettled([
      apiGet<Daily>(`/api/sales/reports/daily?day=${t}`),
      apiGet<Dash>(`/api/sales/reports/dashboard?day=${t}`),
      apiGet<Aging>('/api/finance/reports/aging'),
      apiGet<Pending>('/api/integration/reports/pending-documents'),
    ]).then((r) => {
      if (r[0].status === 'fulfilled') setDaily(r[0].value);
      if (r[1].status === 'fulfilled') setDash(r[1].value);
      if (r[2].status === 'fulfilled') setAging(r[2].value);
      if (r[3].status === 'fulfilled') setPending(r[3].value);
      if (r.every((x) => x.status === 'rejected')) setErr('Veri alınamadı.');
    });
  }, []);

  const cards = [
    { label: 'Bugünkü Satış (net)', value: daily ? formatTRY(daily.net) : '…', sub: daily ? `${daily.count} satış · brüt ${formatTRY(daily.gross)}` : '' },
    { label: 'Bugünkü Komisyon', value: daily ? formatTRY(daily.commission) : '…', sub: '' },
    { label: 'Bekleyen Hakediş', value: dash ? formatTRY(dash.pendingSettlementTotal) : '…', sub: 'ödenmemiş müstahsil' },
    { label: 'Açık Cari (vadesi gelmemiş)', value: aging ? formatTRY(aging.current.amount) : '…', sub: aging ? `${aging.current.accountCount} hesap` : '' },
    { label: 'Bugünkü Mal Geliş', value: dash ? String(dash.todayConsignmentCount) : '…', sub: 'parti' },
    { label: 'Bekleyen e-Belge', value: pending ? String(pending.total) : '…', sub: pending ? `${pending.pendingInvoices} fatura` : '' },
  ];

  return (
    <section className="panel">
      <h2>Kontrol Paneli</h2>
      {err && <p className="error">{err}</p>}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))', gap: 16, marginTop: 8 }}>
        {cards.map((c) => (
          <div key={c.label} style={{ border: '1px solid var(--line)', borderRadius: 10, padding: 16, background: 'var(--panel-2)' }}>
            <div style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 6 }}>{c.label}</div>
            <div style={{ fontSize: 24, fontWeight: 700 }}>{c.value}</div>
            {c.sub && <div style={{ fontSize: 12, color: 'var(--muted)', marginTop: 4 }}>{c.sub}</div>}
          </div>
        ))}
      </div>
    </section>
  );
}

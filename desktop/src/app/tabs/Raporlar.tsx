import { useMemo } from 'react';
import { formatTRY } from '../../lib/format';
import { useFetch } from '../../lib/useFetch';
import { Kpi, KpiGrid } from './ui';

interface SalesSummary { count: number; totalGross: number; totalCommission: number; totalDeductions: number; totalNet: number }
interface CommissionIncome { totalCommission: number; totalVat: number; grandTotal: number }
interface Register { name: string; kind: number; balance: number }
interface Cheque { kind: number; direction: number; amount: number; status: number }
interface Paged<T> { items?: T[] }

const CH_STATUS: Record<number, string> = { 1: 'Portföyde', 2: 'Tahsile verildi', 3: 'Tahsil edildi', 4: 'Karşılıksız', 5: 'Ciro edildi', 6: 'Ödendi' };

function iso(d: Date): string { return d.toISOString().slice(0, 10); }
const TO = iso(new Date());
const FROM = iso(new Date(Date.now() - 30 * 86400000));

export function Raporlar() {
  const sum = useFetch<SalesSummary>(`/api/sales/reports/sales-summary?from=${FROM}&to=${TO}`).data;
  const com = useFetch<CommissionIncome>(`/api/sales/reports/commission-income?from=${FROM}&to=${TO}`).data;
  const regs = useFetch<Register[]>('/api/finance/cash-registers').data ?? [];
  const cheques = useFetch<Paged<Cheque>>('/api/finance/cheques?page=1&pageSize=200').data?.items ?? [];

  const cashTotal = regs.reduce((a, r) => a + r.balance, 0);
  const chequeStats = useMemo(() => {
    const byStatus = new Map<number, { count: number; total: number }>();
    let received = 0, issued = 0;
    for (const c of cheques) {
      const s = byStatus.get(c.status) ?? { count: 0, total: 0 };
      s.count++; s.total += c.amount; byStatus.set(c.status, s);
      if (c.direction === 1) received += c.amount; else issued += c.amount;
    }
    return { byStatus, received, issued, total: received + issued, count: cheques.length };
  }, [cheques]);

  return (
    <section className="panel">
      <h2>Raporlar — Son 30 Gün</h2>

      <h2 style={{ fontSize: 14, marginTop: 8 }}>Satış Özeti</h2>
      <KpiGrid>
        <Kpi label="Satış adedi" value={sum ? String(sum.count) : '…'} />
        <Kpi label="Brüt" value={sum ? formatTRY(sum.totalGross) : '…'} />
        <Kpi label="Komisyon" value={sum ? formatTRY(sum.totalCommission) : '…'} />
        <Kpi label="Kesinti (KDV hariç)" value={sum ? formatTRY(sum.totalDeductions) : '…'} />
        <Kpi label="Müstahsil net" value={sum ? formatTRY(sum.totalNet) : '…'} />
      </KpiGrid>

      <h2 style={{ fontSize: 14, marginTop: 24 }}>Komisyon Geliri</h2>
      <KpiGrid>
        <Kpi label="Komisyon" value={com ? formatTRY(com.totalCommission) : '…'} />
        <Kpi label="Komisyon KDV" value={com ? formatTRY(com.totalVat) : '…'} />
        <Kpi label="Toplam gelir" value={com ? formatTRY(com.grandTotal) : '…'} />
      </KpiGrid>

      <h2 style={{ fontSize: 14, marginTop: 24 }}>Kasa Durumu</h2>
      {regs.length === 0 ? <p className="muted">Kasa yok.</p> : (
        <table>
          <thead><tr><th>Kasa</th><th>Tür</th><th className="num">Bakiye</th></tr></thead>
          <tbody>
            {regs.map((r) => <tr key={r.name}><td>{r.name}</td><td>{r.kind === 1 ? 'Ticari' : 'Rehin'}</td><td className="num">{formatTRY(r.balance)}</td></tr>)}
            <tr><td colSpan={2} style={{ fontWeight: 700 }}>Toplam</td><td className="num" style={{ fontWeight: 700 }}>{formatTRY(cashTotal)}</td></tr>
          </tbody>
        </table>
      )}

      <h2 style={{ fontSize: 14, marginTop: 24 }}>Çek / Senet Portföyü ({chequeStats.count})</h2>
      <KpiGrid>
        <Kpi label="Alınan (toplam)" value={formatTRY(chequeStats.received)} />
        <Kpi label="Verilen (toplam)" value={formatTRY(chequeStats.issued)} />
        <Kpi label="Genel toplam" value={formatTRY(chequeStats.total)} />
      </KpiGrid>
      {chequeStats.count > 0 && (
        <table style={{ marginTop: 12 }}>
          <thead><tr><th>Durum</th><th className="num">Adet</th><th className="num">Tutar</th></tr></thead>
          <tbody>
            {[...chequeStats.byStatus.entries()].sort((a, b) => a[0] - b[0]).map(([st, v]) => (
              <tr key={st}><td>{CH_STATUS[st] ?? st}</td><td className="num">{v.count}</td><td className="num">{formatTRY(v.total)}</td></tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

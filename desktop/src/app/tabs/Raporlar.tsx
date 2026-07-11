import { useEffect, useState } from 'react';
import { apiGet } from '../../lib/api';
import { formatTRY } from '../../lib/format';

interface SalesSummary { count: number; totalGross: number; totalCommission: number; totalDeductions: number; totalNet: number }
interface CommissionIncome { totalCommission: number; totalVat: number; grandTotal: number }

function iso(d: Date): string { return d.toISOString().slice(0, 10); }

export function Raporlar() {
  const [sum, setSum] = useState<SalesSummary | null>(null);
  const [com, setCom] = useState<CommissionIncome | null>(null);

  useEffect(() => {
    const to = new Date();
    const from = new Date(Date.now() - 30 * 86400000);
    apiGet<SalesSummary>(`/api/sales/reports/sales-summary?from=${iso(from)}&to=${iso(to)}`).then(setSum).catch(() => {});
    apiGet<CommissionIncome>(`/api/sales/reports/commission-income?from=${iso(from)}&to=${iso(to)}`).then(setCom).catch(() => {});
  }, []);

  return (
    <section className="panel">
      <h2>Raporlar — Son 30 Gün</h2>
      <h2 style={{ fontSize: 14, marginTop: 8 }}>Satış Özeti</h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px,1fr))', gap: 14 }}>
        <Kpi label="Satış adedi" value={sum ? String(sum.count) : '…'} />
        <Kpi label="Brüt" value={sum ? formatTRY(sum.totalGross) : '…'} />
        <Kpi label="Komisyon" value={sum ? formatTRY(sum.totalCommission) : '…'} />
        <Kpi label="Kesinti (KDV hariç)" value={sum ? formatTRY(sum.totalDeductions) : '…'} />
        <Kpi label="Müstahsil net" value={sum ? formatTRY(sum.totalNet) : '…'} />
      </div>
      <h2 style={{ fontSize: 14, marginTop: 24 }}>Komisyon Geliri</h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px,1fr))', gap: 14 }}>
        <Kpi label="Komisyon" value={com ? formatTRY(com.totalCommission) : '…'} />
        <Kpi label="Komisyon KDV" value={com ? formatTRY(com.totalVat) : '…'} />
        <Kpi label="Toplam gelir" value={com ? formatTRY(com.grandTotal) : '…'} />
      </div>
    </section>
  );
}

function Kpi({ label, value }: { label: string; value: string }) {
  return (
    <div style={{ border: '1px solid var(--line)', borderRadius: 10, padding: 16, background: 'var(--panel-2)' }}>
      <div style={{ fontSize: 12, color: 'var(--muted)' }}>{label}</div>
      <div style={{ fontSize: 22, fontWeight: 700 }}>{value}</div>
    </div>
  );
}

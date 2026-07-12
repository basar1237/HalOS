import { useEffect, useState } from 'react';
import { apiGet } from '../../lib/api';
import { formatTRY } from '../../lib/format';
import { Kpi, KpiGrid } from './ui';

interface Daily { count: number; gross: number; commission: number; net: number }
interface Dash { todayConsignmentCount: number; pendingSettlementTotal: number }
interface AgingBucket { amount: number; accountCount: number }
interface Aging { current: AgingBucket }
interface Pending { pendingInvoices: number; pendingProducerReceipts: number; total: number }
interface Register { balance: number }
interface Cheque { amount: number; status: number; dueDate: string }
interface Paged<T> { items?: T[] }

function todayStr(): string { return new Date().toISOString().slice(0, 10); }
function overdue(iso: string): boolean {
  const due = new Date(iso); due.setHours(0, 0, 0, 0);
  const t = new Date(); t.setHours(0, 0, 0, 0);
  return due.getTime() < t.getTime();
}

export function Dashboard() {
  const [daily, setDaily] = useState<Daily | null>(null);
  const [dash, setDash] = useState<Dash | null>(null);
  const [aging, setAging] = useState<Aging | null>(null);
  const [pending, setPending] = useState<Pending | null>(null);
  const [cashTotal, setCashTotal] = useState<number | null>(null);
  const [chequePortfolio, setChequePortfolio] = useState<{ total: number; count: number; overdue: number } | null>(null);

  useEffect(() => {
    const t = todayStr();
    apiGet<Daily>(`/api/sales/reports/daily?day=${t}`).then(setDaily).catch(() => {});
    apiGet<Dash>(`/api/sales/reports/dashboard?day=${t}`).then(setDash).catch(() => {});
    apiGet<Aging>('/api/finance/reports/aging').then(setAging).catch(() => {});
    apiGet<Pending>('/api/integration/reports/pending-documents').then(setPending).catch(() => {});
    apiGet<Register[]>('/api/finance/cash-registers')
      .then((rs) => setCashTotal(rs.reduce((a, r) => a + (r.balance || 0), 0)))
      .catch(() => {});
    apiGet<Paged<Cheque>>('/api/finance/cheques?page=1&pageSize=200')
      .then((r) => {
        const open = (r.items ?? []).filter((c) => c.status === 1 || c.status === 2);
        setChequePortfolio({
          total: open.reduce((a, c) => a + c.amount, 0),
          count: open.length,
          overdue: open.filter((c) => overdue(c.dueDate)).length,
        });
      })
      .catch(() => {});
  }, []);

  const cards = [
    { label: 'Bugünkü Satış (net)', value: daily ? formatTRY(daily.net) : '…', sub: daily ? `${daily.count} satış · brüt ${formatTRY(daily.gross)}` : '' },
    { label: 'Bugünkü Komisyon', value: daily ? formatTRY(daily.commission) : '…', sub: '' },
    { label: 'Bekleyen Hakediş', value: dash ? formatTRY(dash.pendingSettlementTotal) : '…', sub: 'ödenmemiş müstahsil' },
    { label: 'Açık Cari', value: aging ? formatTRY(aging.current.amount) : '…', sub: aging ? `${aging.current.accountCount} hesap` : '' },
    { label: 'Kasa Toplam Bakiye', value: cashTotal != null ? formatTRY(cashTotal) : '…', sub: 'tüm kasalar' },
    { label: 'Portföydeki Çek/Senet', value: chequePortfolio ? formatTRY(chequePortfolio.total) : '…', sub: chequePortfolio ? `${chequePortfolio.count} adet` : '' },
    { label: 'Vadesi Geçmiş Çek', value: chequePortfolio ? String(chequePortfolio.overdue) : '…', sub: 'takip gerekli', danger: !!chequePortfolio && chequePortfolio.overdue > 0 },
    { label: 'Bugünkü Mal Geliş', value: dash ? String(dash.todayConsignmentCount) : '…', sub: 'parti' },
    { label: 'Bekleyen e-Belge', value: pending ? String(pending.total) : '…', sub: pending ? `${pending.pendingInvoices} fatura` : '' },
  ];

  return (
    <section className="panel">
      <h2>Kontrol Paneli</h2>
      <KpiGrid>
        {cards.map((c) => (
          <Kpi key={c.label} label={c.label} value={c.value} sub={c.sub} danger={c.danger} />
        ))}
      </KpiGrid>
    </section>
  );
}

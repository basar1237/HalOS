import { useEffect, useState } from 'react';
import { apiGet } from '../../lib/api';

interface Paged<T> { items?: T[]; totalCount?: number }
interface Passport { id: string; kunyeNo?: string; productName?: string; status?: number }

const STATUS: Record<number, string> = { 1: 'Hazır', 2: 'Gönderildi', 3: 'Onaylandı', 4: 'Reddedildi' };

export function EBelge() {
  const [inv, setInv] = useState<number | null>(null);
  const [emm, setEmm] = useState<number | null>(null);
  const [passports, setPassports] = useState<Passport[]>([]);
  const [passTotal, setPassTotal] = useState<number | null>(null);

  useEffect(() => {
    apiGet<Paged<unknown>>('/api/integration/invoices?page=1&pageSize=1').then((r) => setInv(r.totalCount ?? 0)).catch(() => {});
    apiGet<Paged<unknown>>('/api/integration/producer-receipts?page=1&pageSize=1').then((r) => setEmm(r.totalCount ?? 0)).catch(() => {});
    apiGet<Paged<Passport>>('/api/integration/product-passports?page=1&pageSize=30').then((r) => { setPassports(r.items ?? []); setPassTotal(r.totalCount ?? 0); }).catch(() => {});
  }, []);

  const cards = [
    { label: 'e-Fatura', value: inv ?? '…' },
    { label: 'e-Müstahsil Makbuzu', value: emm ?? '…' },
    { label: 'Künye (HKS)', value: passTotal ?? '…' },
  ];

  return (
    <section className="panel">
      <h2>e-Belge &amp; HKS</h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px,1fr))', gap: 14, margin: '8px 0 20px' }}>
        {cards.map((c) => (
          <div key={c.label} style={{ border: '1px solid var(--line)', borderRadius: 10, padding: 16, background: 'var(--panel-2)' }}>
            <div style={{ fontSize: 12, color: 'var(--muted)' }}>{c.label}</div>
            <div style={{ fontSize: 26, fontWeight: 700 }}>{c.value}</div>
          </div>
        ))}
      </div>
      <h2 style={{ fontSize: 14 }}>Son Künyeler</h2>
      {passports.length === 0 ? (
        <p className="muted">Künye kaydı yok.</p>
      ) : (
        <table>
          <thead><tr><th>Künye No</th><th>Ürün</th><th>Durum</th></tr></thead>
          <tbody>
            {passports.map((p) => (
              <tr key={p.id}>
                <td style={{ fontFamily: 'ui-monospace, monospace' }}>{p.kunyeNo ?? '—'}</td>
                <td>{p.productName ?? '—'}</td>
                <td>{p.status != null ? (STATUS[p.status] ?? p.status) : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      <p className="muted" style={{ marginTop: 12 }}>Gerçek GİB/HKS kesimi entegratör kimliği gerektirir (Faz D).</p>
    </section>
  );
}

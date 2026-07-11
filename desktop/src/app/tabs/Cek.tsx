import { useEffect, useState } from 'react';
import { apiGet, apiPost } from '../../lib/api';
import { formatTRY } from '../../lib/format';

interface Cheque {
  id: string; kind: number; direction: number; bankName: string; serialNo: string;
  amount: number; issueDate: string; dueDate: string; status: number; note?: string | null;
}
interface Paged<T> { items?: T[]; totalCount?: number }

const KIND: Record<number, string> = { 1: 'Çek', 2: 'Senet' };
const DIR: Record<number, string> = { 1: 'Alınan', 2: 'Verilen' };
const STATUS: Record<number, string> = { 1: 'Portföyde', 2: 'Tahsile verildi', 3: 'Tahsil edildi', 4: 'Karşılıksız', 5: 'Ciro edildi', 6: 'Ödendi' };
const STATUS_TAG: Record<number, string> = { 1: '', 2: 'pending', 3: 'synced', 4: 'conflict', 5: 'pending', 6: 'synced' };

function d(iso: string) { const x = new Date(iso); return isNaN(x.getTime()) ? '—' : x.toLocaleDateString('tr-TR'); }
function todayIso() { return new Date().toISOString().slice(0, 10); }

export function Cek() {
  const [rows, setRows] = useState<Cheque[]>([]);
  const [msg, setMsg] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [form, setForm] = useState({ kind: 1, direction: 1, bankName: '', serialNo: '', amount: '', dueDate: todayIso(), note: '' });

  async function load() {
    try { const r = await apiGet<Paged<Cheque>>('/api/finance/cheques?page=1&pageSize=100'); setRows(r.items ?? []); }
    catch { setMsg('Liste alınamadı.'); }
  }
  useEffect(() => { void load(); }, []);

  async function register() {
    if (!(Number(form.amount) > 0)) { setMsg('Tutar girin.'); return; }
    setBusy(true); setMsg(null);
    try {
      await apiPost('/api/finance/cheques', {
        kind: form.kind, direction: form.direction, partyId: null,
        bankName: form.bankName, serialNo: form.serialNo, amount: Number(form.amount),
        issueDate: new Date().toISOString(), dueDate: new Date(form.dueDate).toISOString(), note: form.note || null,
      });
      setForm({ ...form, bankName: '', serialNo: '', amount: '', note: '' });
      setMsg('✓ Kaydedildi.');
      await load();
    } catch (e) { setMsg((e as { message?: string }).message ?? 'Kaydedilemedi.'); }
    finally { setBusy(false); }
  }

  async function changeStatus(id: string, newStatus: number) {
    try { await apiPost(`/api/finance/cheques/${id}/status`, { newStatus }); await load(); }
    catch (e) { setMsg((e as { message?: string }).message ?? 'Durum değişmedi.'); }
  }

  const set = (k: string, v: string | number) => setForm((f) => ({ ...f, [k]: v }));

  return (
    <section className="panel">
      <h2>Çek / Senet ({rows.length})</h2>

      <div className="xg-head" style={{ marginBottom: 14 }}>
        <div><label>Tür</label><select value={form.kind} onChange={(e) => set('kind', Number(e.target.value))}><option value={1}>Çek</option><option value={2}>Senet</option></select></div>
        <div><label>Yön</label><select value={form.direction} onChange={(e) => set('direction', Number(e.target.value))}><option value={1}>Alınan</option><option value={2}>Verilen</option></select></div>
        <div><label>Banka</label><input value={form.bankName} onChange={(e) => set('bankName', e.target.value)} placeholder="Ziraat…" /></div>
        <div><label>Seri No</label><input value={form.serialNo} onChange={(e) => set('serialNo', e.target.value)} /></div>
        <div><label>Tutar (₺)</label><input inputMode="decimal" value={form.amount} onChange={(e) => set('amount', e.target.value)} /></div>
        <div><label>Vade</label><input type="date" value={form.dueDate} onChange={(e) => set('dueDate', e.target.value)} /></div>
        <div style={{ display: 'flex', alignItems: 'flex-end' }}><button onClick={() => void register()} disabled={busy} style={{ width: '100%' }}>{busy ? 'Kaydediliyor…' : 'Portföye Ekle'}</button></div>
      </div>
      {msg && <p className="muted">{msg}</p>}

      {rows.length === 0 ? (
        <p className="muted">Çek/senet kaydı yok.</p>
      ) : (
        <table>
          <thead><tr><th>Tür</th><th>Yön</th><th>Banka</th><th>Seri</th><th className="num">Tutar</th><th>Vade</th><th>Durum</th><th>İşlem</th></tr></thead>
          <tbody>
            {rows.map((c) => (
              <tr key={c.id}>
                <td>{KIND[c.kind]}</td>
                <td>{DIR[c.direction]}</td>
                <td>{c.bankName || '—'}</td>
                <td>{c.serialNo || '—'}</td>
                <td className="num">{formatTRY(c.amount)}</td>
                <td>{d(c.dueDate)}</td>
                <td><span className={`tag ${STATUS_TAG[c.status] || ''}`}>{STATUS[c.status]}</span></td>
                <td>
                  <select value={c.status} onChange={(e) => void changeStatus(c.id, Number(e.target.value))} style={{ padding: '4px 8px', fontSize: 12 }}>
                    {Object.entries(STATUS).map(([v, n]) => <option key={v} value={v}>{n}</option>)}
                  </select>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

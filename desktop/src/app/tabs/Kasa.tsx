import { useEffect, useState } from 'react';
import { apiGet, apiPost } from '../../lib/api';
import { formatTRY } from '../../lib/format';

interface Register { id: string; name: string; kind: number; balance: number; movementCount: number }
const KIND: Record<number, string> = { 1: 'Ticari', 2: 'Rehin' };

export function Kasa() {
  const [rows, setRows] = useState<Register[]>([]);
  const [msg, setMsg] = useState<string | null>(null);
  const [newName, setNewName] = useState('');
  const [newKind, setNewKind] = useState(1);
  // hareket
  const [mvReg, setMvReg] = useState('');
  const [mvDir, setMvDir] = useState(1);
  const [mvAmount, setMvAmount] = useState('');
  const [mvDesc, setMvDesc] = useState('');
  // virman
  const [vFrom, setVFrom] = useState('');
  const [vTo, setVTo] = useState('');
  const [vAmount, setVAmount] = useState('');

  async function load() {
    try { setRows(await apiGet<Register[]>('/api/finance/cash-registers')); }
    catch { setMsg('Kasa listesi alınamadı.'); }
  }
  useEffect(() => { void load(); }, []);

  async function openReg() {
    if (!newName.trim()) { setMsg('Kasa adı girin.'); return; }
    try { await apiPost('/api/finance/cash-registers', { name: newName.trim(), kind: newKind }); setNewName(''); setMsg('✓ Kasa açıldı.'); await load(); }
    catch (e) { setMsg((e as { message?: string }).message ?? 'Açılamadı.'); }
  }
  async function record() {
    if (!mvReg || !(Number(mvAmount) > 0)) { setMsg('Kasa ve tutar girin.'); return; }
    try { await apiPost(`/api/finance/cash-registers/${mvReg}/movements`, { direction: mvDir, amount: Number(mvAmount), description: mvDesc || null, occurredAt: null }); setMvAmount(''); setMvDesc(''); setMsg('✓ Hareket işlendi.'); await load(); }
    catch (e) { setMsg((e as { message?: string }).message ?? 'İşlenemedi.'); }
  }
  async function transfer() {
    if (!vFrom || !vTo || !(Number(vAmount) > 0)) { setMsg('Kaynak/hedef kasa ve tutar girin.'); return; }
    try { await apiPost('/api/finance/cash-registers/transfer', { fromRegisterId: vFrom, toRegisterId: vTo, amount: Number(vAmount), description: null, occurredAt: null }); setVAmount(''); setMsg('✓ Virman yapıldı.'); await load(); }
    catch (e) { setMsg((e as { message?: string }).message ?? 'Virman başarısız.'); }
  }

  return (
    <section className="panel">
      <h2>Kasa ({rows.length})</h2>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px,1fr))', gap: 14, margin: '8px 0 20px' }}>
        {rows.map((r) => (
          <div key={r.id} style={{ border: '1px solid var(--line)', borderRadius: 10, padding: 16, background: 'var(--panel-2)' }}>
            <div style={{ fontSize: 12, color: 'var(--muted)' }}>{r.name} · {KIND[r.kind]}</div>
            <div style={{ fontSize: 24, fontWeight: 700 }}>{formatTRY(r.balance)}</div>
            <div style={{ fontSize: 12, color: 'var(--muted)' }}>{r.movementCount} hareket</div>
          </div>
        ))}
        {rows.length === 0 && <p className="muted">Kasa yok.</p>}
      </div>

      {msg && <p className="muted">{msg}</p>}

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px,1fr))', gap: 16 }}>
        <fieldset style={{ border: '1px solid var(--line)', borderRadius: 10, padding: 14 }}>
          <legend style={{ fontSize: 13, fontWeight: 600 }}>Yeni Kasa</legend>
          <div className="row"><div><label>Ad</label><input value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="Merkez Kasa" /></div>
            <div><label>Tür</label><select value={newKind} onChange={(e) => setNewKind(Number(e.target.value))}><option value={1}>Ticari</option><option value={2}>Rehin</option></select></div></div>
          <button onClick={() => void openReg()} style={{ width: '100%' }}>Kasa Aç</button>
        </fieldset>

        <fieldset style={{ border: '1px solid var(--line)', borderRadius: 10, padding: 14 }}>
          <legend style={{ fontSize: 13, fontWeight: 600 }}>Tahsil / Tediye</legend>
          <div className="row"><div><label>Kasa</label><select value={mvReg} onChange={(e) => setMvReg(e.target.value)}><option value="">seçin</option>{rows.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}</select></div>
            <div><label>Yön</label><select value={mvDir} onChange={(e) => setMvDir(Number(e.target.value))}><option value={1}>Tahsil (+)</option><option value={2}>Tediye (−)</option></select></div></div>
          <div className="row"><div><label>Tutar (₺)</label><input inputMode="decimal" value={mvAmount} onChange={(e) => setMvAmount(e.target.value)} /></div>
            <div><label>Açıklama</label><input value={mvDesc} onChange={(e) => setMvDesc(e.target.value)} /></div></div>
          <button onClick={() => void record()} style={{ width: '100%' }}>İşle</button>
        </fieldset>

        <fieldset style={{ border: '1px solid var(--line)', borderRadius: 10, padding: 14 }}>
          <legend style={{ fontSize: 13, fontWeight: 600 }}>Virman (kasalar arası)</legend>
          <div className="row"><div><label>Kaynak</label><select value={vFrom} onChange={(e) => setVFrom(e.target.value)}><option value="">seçin</option>{rows.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}</select></div>
            <div><label>Hedef</label><select value={vTo} onChange={(e) => setVTo(e.target.value)}><option value="">seçin</option>{rows.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}</select></div></div>
          <div><label>Tutar (₺)</label><input inputMode="decimal" value={vAmount} onChange={(e) => setVAmount(e.target.value)} /></div>
          <button onClick={() => void transfer()} style={{ width: '100%', marginTop: 8 }}>Virman Yap</button>
        </fieldset>
      </div>
    </section>
  );
}

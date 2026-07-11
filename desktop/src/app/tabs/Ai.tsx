import { useState } from 'react';
import { apiPost } from '../../lib/api';

interface AskResponse { answer: string; model?: string }

export function Ai() {
  const [q, setQ] = useState('');
  const [answer, setAnswer] = useState<string | null>(null);
  const [model, setModel] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function ask() {
    if (!q.trim() || busy) return;
    setBusy(true);
    setErr(null);
    setAnswer(null);
    try {
      const res = await apiPost<AskResponse>('/ai/ask', { question: q.trim() });
      setAnswer(res.answer);
      setModel(res.model ?? null);
    } catch (e) {
      setErr((e as { message?: string }).message ?? 'AI yanıt vermedi.');
    } finally {
      setBusy(false);
    }
  }

  const samples = ['Bu ay satışlarım nasıl?', 'Toplam komisyon gelirim ne kadar?', 'En çok hangi üründen sattım?'];

  return (
    <section className="panel">
      <h2>AI Muhasebeci</h2>
      <p className="muted" style={{ marginTop: -6 }}>Yerel/çevrimdışı yapay zeka — veriniz makinede kalır. Doğal dille sorun.</p>
      <div className="row" style={{ marginTop: 12 }}>
        <input
          value={q}
          placeholder="Örn: Bu ay kârım ne kadar?"
          onChange={(e) => setQ(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') void ask(); }}
        />
        <div style={{ flex: '0 0 auto' }}>
          <button onClick={() => void ask()} disabled={busy}>{busy ? 'Düşünüyor…' : 'Sor'}</button>
        </div>
      </div>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
        {samples.map((s) => (
          <button key={s} className="ghost" style={{ fontSize: 12, padding: '5px 10px' }} onClick={() => setQ(s)}>{s}</button>
        ))}
      </div>
      {err && <p className="error">{err}</p>}
      {busy && <p className="muted">Yapay zeka yanıt hazırlıyor (yerel model, biraz sürebilir)…</p>}
      {answer && (
        <div style={{ border: '1px solid var(--line)', borderRadius: 10, padding: 16, background: 'var(--panel-2)', whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
          {answer}
          {model && <div className="muted" style={{ marginTop: 10, fontSize: 12 }}>model: {model}</div>}
        </div>
      )}
    </section>
  );
}

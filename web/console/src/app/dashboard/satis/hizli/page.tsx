'use client';

// Hızlı Satış — Excel-hızında, klavye-navigasyonlu satış giriş gridi. GERÇEK backend'e bağlı:
// ürün/taraf listeleri API'den gelir, "Kaydet" createCompleteSale ile satışı oluşturur
// (komisyon/stopaj/hakediş sunucuda hesaplanır, e-belge tetiklenir).
//
// Klavye: ↑/↓ satır gez · Enter alt satır (son satırdaysa yeni) · Tab yatay · F2 yeni satır
// · Shift+Del satır sil · Ctrl+Enter kaydet.

import { useMemo, useRef, useState, type KeyboardEvent } from 'react';
import { useRouter } from 'next/navigation';

import { usePartyOptions } from '@/features/parties/use-party-options';
import { useProductOptions } from '@/features/products/use-product-options';
import {
  createCompleteSale,
  SaleTerm,
  UNIT_LABEL,
  type SaleLineInput,
} from '@/features/sales/sales-api';
import { isApiError } from '@/lib/api-client';

interface Row {
  productId: string;
  productName: string;
  qty: string;
  unit: number;
  price: string;
}
const emptyRow = (): Row => ({ productId: '', productName: '', qty: '', unit: 1, price: '' });

function round2(n: number): number {
  const x = n * 100;
  const f = Math.floor(x);
  const d = x - f;
  const r = d > 0.5 ? f + 1 : d < 0.5 ? f : f % 2 === 0 ? f : f + 1;
  return r / 100;
}
const TL = (n: number) =>
  n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';

// Tahmini kesinti oranları (kesin tutar sunucuda hesaplanır — kaynak-doğruluk backend).
const RATES = { komisyon: 0.08, stopaj: 0.02, bagkur: 0.01, rusum: 0.01 };

export default function FastSalePage() {
  const router = useRouter();
  const { options: products, loading: productsLoading } = useProductOptions();
  const { options: parties, loading: partiesLoading } = usePartyOptions();

  const [buyerId, setBuyerId] = useState('');
  const [producerId, setProducerId] = useState('');
  const [term, setTerm] = useState<number>(SaleTerm.Cash);
  const [isWithinMarket, setIsWithinMarket] = useState(true);
  const [rows, setRows] = useState<Row[]>([emptyRow(), emptyRow(), emptyRow()]);
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);

  const cellRefs = useRef<Map<string, HTMLElement>>(new Map());
  const setRef = (r: number, c: number) => (el: HTMLElement | null) => {
    const k = `${r}-${c}`;
    if (el) cellRefs.current.set(k, el);
    else cellRefs.current.delete(k);
  };
  const focusCell = (r: number, c: number) => {
    const el = cellRefs.current.get(`${r}-${c}`);
    if (el) (el as HTMLInputElement).focus();
  };

  const update = (i: number, patch: Partial<Row>) =>
    setRows((prev) => prev.map((row, idx) => (idx === i ? { ...row, ...patch } : row)));
  const addRow = () => setRows((prev) => [...prev, emptyRow()]);
  const removeRow = (i: number) =>
    setRows((prev) => (prev.length > 1 ? prev.filter((_, idx) => idx !== i) : prev));

  function onProductInput(i: number, value: string) {
    const match = products.find((p) => p.name.toLowerCase() === value.toLowerCase());
    if (match) update(i, { productId: match.id, productName: match.name, unit: match.defaultUnit });
    else update(i, { productId: '', productName: value });
  }

  function onKeyDown(e: KeyboardEvent, r: number, c: number) {
    if (e.key === 'Enter' && e.ctrlKey) { e.preventDefault(); void handleSave(); return; }
    if (e.key === 'F2') { e.preventDefault(); addRow(); setTimeout(() => focusCell(rows.length, 0), 0); return; }
    if (e.key === 'ArrowDown' || e.key === 'Enter') {
      e.preventDefault();
      if (r === rows.length - 1) { addRow(); setTimeout(() => focusCell(r + 1, c), 0); }
      else focusCell(r + 1, c);
      return;
    }
    if (e.key === 'ArrowUp') { e.preventDefault(); focusCell(r - 1, c); return; }
    if (e.key === 'Delete' && e.shiftKey) {
      e.preventDefault(); removeRow(r); setTimeout(() => focusCell(Math.max(0, r - 1), c), 0);
    }
  }

  const calc = useMemo(() => {
    const lineTotals = rows.map((r) => round2((Number(r.qty) || 0) * (Number(r.price) || 0)));
    const brut = round2(lineTotals.reduce((a, b) => a + b, 0));
    const komisyon = round2(brut * RATES.komisyon);
    const stopaj = round2(brut * RATES.stopaj);
    const bagkur = round2(brut * RATES.bagkur);
    const rusum = round2(brut * RATES.rusum);
    const net = round2(brut - komisyon - stopaj - bagkur - rusum);
    return { lineTotals, brut, komisyon, stopaj, bagkur, rusum, net };
  }, [rows]);

  async function handleSave() {
    setMsg(null);
    if (!buyerId || !producerId) {
      setMsg({ ok: false, text: 'Alıcı ve müstahsil seçilmelidir.' });
      return;
    }
    const lines: SaleLineInput[] = rows
      .filter((r) => r.productId && Number(r.qty) > 0 && Number(r.price) > 0)
      .map((r) => ({ productId: r.productId, quantity: Number(r.qty), unit: r.unit, unitPrice: Number(r.price) }));
    if (!lines.length) {
      setMsg({ ok: false, text: 'En az bir geçerli satır girin (ürün + miktar + fiyat).' });
      return;
    }
    setSaving(true);
    try {
      await createCompleteSale(
        { buyerPartyId: buyerId, producerPartyId: producerId, soldAt: new Date().toISOString(), isWithinMarket, term },
        lines,
      );
      setMsg({ ok: true, text: '✓ Satış kaydedildi — komisyon/hakediş hesaplandı, e-belge tetiklendi.' });
      setRows([emptyRow(), emptyRow(), emptyRow()]);
      setTimeout(() => router.push('/dashboard/satis'), 900);
    } catch (err) {
      setMsg({ ok: false, text: isApiError(err) ? err.message : 'Satış kaydedilemedi.' });
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="fs-page">
      <div className="page-head">
        <h1 className="page-title">Hızlı Satış</h1>
        <button className="btn-primary btn-inline" onClick={() => void handleSave()} disabled={saving}>
          {saving ? 'Kaydediliyor…' : 'Kaydet & Tamamla'} <span style={{ opacity: 0.7, fontSize: 12 }}>(Ctrl+Enter)</span>
        </button>
      </div>

      <p className="page-lead">
        Klavyeyle hızlı satış girişi — fare gerekmez. Üründe yazmaya başla, Tab/Enter ile ilerle,
        son satırda Enter yeni satır açar. Kaydet: komisyon/stopaj/hakediş sunucuda hesaplanır.
      </p>

      <div className="fs-head">
        <div className="fs-field">
          <label>Alıcı</label>
          <select value={buyerId} onChange={(e) => setBuyerId(e.target.value)} disabled={partiesLoading}>
            <option value="">{partiesLoading ? 'Yükleniyor…' : 'Seçiniz…'}</option>
            {parties.map((p) => <option key={p.id} value={p.id}>{p.displayName}</option>)}
          </select>
        </div>
        <div className="fs-field">
          <label>Müstahsil</label>
          <select value={producerId} onChange={(e) => setProducerId(e.target.value)} disabled={partiesLoading}>
            <option value="">{partiesLoading ? 'Yükleniyor…' : 'Seçiniz…'}</option>
            {parties.map((p) => <option key={p.id} value={p.id}>{p.displayName}</option>)}
          </select>
        </div>
        <div className="fs-field">
          <label>Vade</label>
          <select value={term} onChange={(e) => setTerm(Number(e.target.value))}>
            <option value={SaleTerm.Cash}>Peşin (15 iş günü)</option>
            <option value={SaleTerm.Deferred}>Vadeli (30 gün)</option>
          </select>
        </div>
        <div className="fs-field">
          <label>Satış Tipi</label>
          <select value={isWithinMarket ? '1' : '0'} onChange={(e) => setIsWithinMarket(e.target.value === '1')}>
            <option value="1">Hal içi</option>
            <option value="0">Hal dışı</option>
          </select>
        </div>
      </div>

      <div className="fs-hint">
        <span><kbd>↑</kbd><kbd>↓</kbd> gez</span>
        <span><kbd>Enter</kbd> alt satır</span>
        <span><kbd>Tab</kbd> yatay</span>
        <span><kbd>F2</kbd> yeni satır</span>
        <span><kbd>Shift</kbd>+<kbd>Del</kbd> sil</span>
        <span><kbd>Ctrl</kbd>+<kbd>Enter</kbd> kaydet</span>
      </div>

      <div className="fs-grid-wrap">
        <table className="fs-grid">
          <thead>
            <tr>
              <th className="fs-rownum">#</th>
              <th style={{ width: '34%' }}>Ürün / Cins</th>
              <th style={{ width: '14%' }}>Miktar</th>
              <th style={{ width: '14%' }}>Birim</th>
              <th style={{ width: '16%' }}>Birim Fiyat</th>
              <th style={{ width: '18%' }}>Tutar</th>
              <th style={{ width: 40 }} />
            </tr>
          </thead>
          <tbody>
            {rows.map((row, i) => (
              <tr key={i}>
                <td className="fs-rownum">{i + 1}</td>
                <td>
                  <input
                    ref={setRef(i, 0)} className="fs-cell" list="fs-products"
                    value={row.productName} placeholder={productsLoading ? 'Yükleniyor…' : 'ürün yaz…'}
                    onChange={(e) => onProductInput(i, e.target.value)}
                    onKeyDown={(e) => onKeyDown(e, i, 0)}
                  />
                </td>
                <td>
                  <input ref={setRef(i, 1)} className="fs-cell fs-cell--num" inputMode="decimal"
                    value={row.qty} onChange={(e) => update(i, { qty: e.target.value })}
                    onKeyDown={(e) => onKeyDown(e, i, 1)} />
                </td>
                <td>
                  <select ref={setRef(i, 2)} className="fs-cell" value={row.unit}
                    onChange={(e) => update(i, { unit: Number(e.target.value) })}
                    onKeyDown={(e) => onKeyDown(e, i, 2)}>
                    {Object.entries(UNIT_LABEL).map(([v, n]) => <option key={v} value={v}>{n}</option>)}
                  </select>
                </td>
                <td>
                  <input ref={setRef(i, 3)} className="fs-cell fs-cell--num" inputMode="decimal"
                    value={row.price} onChange={(e) => update(i, { price: e.target.value })}
                    onKeyDown={(e) => onKeyDown(e, i, 3)} />
                </td>
                <td><div className="fs-cell--readonly">{TL(calc.lineTotals[i] || 0)}</div></td>
                <td>
                  <button className="fs-del" onClick={() => removeRow(i)} tabIndex={-1} aria-label="Sil">✕</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <datalist id="fs-products">
          {products.map((p) => <option key={p.id} value={p.name} />)}
        </datalist>
      </div>

      <div><button className="btn-secondary btn-sm" onClick={addRow}>+ Satır ekle (F2)</button></div>

      <div className="fs-totals">
        <div className="fs-total"><p className="fs-total__label">Brüt</p><p className="fs-total__value">{TL(calc.brut)}</p></div>
        <div className="fs-total"><p className="fs-total__label">Komisyon %8</p><p className="fs-total__value">{TL(calc.komisyon)}</p></div>
        <div className="fs-total"><p className="fs-total__label">Stopaj %2</p><p className="fs-total__value">{TL(calc.stopaj)}</p></div>
        <div className="fs-total"><p className="fs-total__label">Bağ-Kur %1</p><p className="fs-total__value">{TL(calc.bagkur)}</p></div>
        <div className="fs-total"><p className="fs-total__label">Rüsum %1</p><p className="fs-total__value">{TL(calc.rusum)}</p></div>
        <div className="fs-total fs-total--net"><p className="fs-total__label">Müstahsil Net (tahmini)</p><p className="fs-total__value">{TL(calc.net)}</p></div>
      </div>

      {msg ? (
        <div className={msg.ok ? 'form-warn' : 'form-error'} style={msg.ok ? { background: '#f0fdf4', borderColor: '#bbf7d0', color: '#166534' } : undefined}>
          {msg.text}
        </div>
      ) : null}
    </div>
  );
}

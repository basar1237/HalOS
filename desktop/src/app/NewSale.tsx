import { useMemo, useRef, useState, type KeyboardEvent } from 'react';
import { commitSaleOffline, maxSeqForAggregate, type LocalDb } from '../lib/db';
import { formatTRY } from '../lib/format';
import { newOperationId } from '../lib/id';
import { grossTotal } from '../lib/money';
import type {
  CachedParty,
  CachedProduct,
  SaleLineInput,
  SaleTerm,
  UnitOfMeasure,
} from '../lib/types';

const UNIT_LABELS: Record<UnitOfMeasure, string> = {
  1: 'kg',
  2: 'adet',
  3: 'kasa',
  4: 'bağ',
  5: 'demet',
};

// Tahmini kesinti oranları (kesin tutar sunucuda hesaplanır).
const RATES = { komisyon: 0.08, stopaj: 0.02, bagkur: 0.01, rusum: 0.01 };

interface Props {
  db: LocalDb | null;
  products: CachedProduct[];
  parties: CachedParty[];
  online: boolean;
  onCommitted: () => void | Promise<void>;
}

interface Row {
  productId: string;
  productName: string;
  quantity: string;
  unitCode: UnitOfMeasure;
  unitPrice: string;
}
const emptyRow = (): Row => ({ productId: '', productName: '', quantity: '', unitCode: 3, unitPrice: '' });

function round2(n: number): number {
  const x = n * 100;
  const f = Math.floor(x);
  const d = x - f;
  const r = d > 0.5 ? f + 1 : d < 0.5 ? f : f % 2 === 0 ? f : f + 1;
  return r / 100;
}

export function NewSale({ db, products, parties, online, onCommitted }: Props) {
  const buyers = useMemo(() => parties.filter((p) => p.partyType === 2), [parties]);
  const producers = useMemo(() => parties.filter((p) => p.partyType === 1), [parties]);
  const [partyId, setPartyId] = useState('');
  const [producerId, setProducerId] = useState('');
  const [saleTerm, setSaleTerm] = useState<SaleTerm>(1);
  const [isWithinMarket, setIsWithinMarket] = useState(true);
  const [rows, setRows] = useState<Row[]>([emptyRow(), emptyRow(), emptyRow()]);
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

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
    if (match) update(i, { productId: match.id, productName: match.name, unitCode: match.defaultUnit });
    else update(i, { productId: '', productName: value });
  }

  function onKeyDown(e: KeyboardEvent, r: number, c: number) {
    if (e.key === 'Enter' && e.ctrlKey) { e.preventDefault(); void submit(); return; }
    if (e.key === 'F2') { e.preventDefault(); addRow(); setTimeout(() => focusCell(rows.length, 0), 0); return; }
    if (e.key === 'ArrowDown' || e.key === 'Enter') {
      e.preventDefault();
      if (r === rows.length - 1) { addRow(); setTimeout(() => focusCell(r + 1, c), 0); }
      else focusCell(r + 1, c);
      return;
    }
    if (e.key === 'ArrowUp') { e.preventDefault(); focusCell(r - 1, c); return; }
    if (e.key === 'Delete' && e.shiftKey) { e.preventDefault(); removeRow(r); setTimeout(() => focusCell(Math.max(0, r - 1), c), 0); }
  }

  const numericLines: SaleLineInput[] = rows
    .filter((l) => l.productId && Number(l.quantity) > 0 && Number(l.unitPrice) > 0)
    .map((l) => ({
      productId: l.productId,
      productName: l.productName,
      quantity: Number(l.quantity),
      unitCode: l.unitCode,
      unitPrice: Number(l.unitPrice),
    }));

  const calc = useMemo(() => {
    const lineTotals = rows.map((r) => round2((Number(r.quantity) || 0) * (Number(r.unitPrice) || 0)));
    const brut = round2(lineTotals.reduce((a, b) => a + b, 0));
    const komisyon = round2(brut * RATES.komisyon);
    const kesinti = round2(brut * (RATES.komisyon + RATES.stopaj + RATES.bagkur + RATES.rusum));
    return { lineTotals, brut, komisyon, net: round2(brut - kesinti) };
  }, [rows]);

  const canSubmit = !!db && !!partyId && !!producerId && numericLines.length > 0 && !busy;

  async function submit() {
    if (!db || !partyId || !producerId) {
      setMsg('Alıcı ve müstahsil seçilmelidir.');
      return;
    }
    if (numericLines.length === 0) {
      setMsg('En az bir geçerli satır girin (ürün + miktar + fiyat).');
      return;
    }
    setBusy(true);
    setMsg(null);
    try {
      const operationId = newOperationId();
      const party = buyers.find((b) => b.id === partyId);
      const createdAt = new Date().toISOString();
      const seq = (await maxSeqForAggregate(db, operationId)) + 1;
      await commitSaleOffline(
        db,
        {
          operationId,
          partyId,
          partyName: party?.name ?? '',
          producerPartyId: producerId,
          saleTerm,
          isWithinMarket,
          lines: numericLines,
          grossTotal: grossTotal(numericLines),
          status: 'completed',
          syncStatus: 'pending',
          createdAt,
        },
        seq,
      );
      setRows([emptyRow(), emptyRow(), emptyRow()]);
      setPartyId('');
      setProducerId('');
      setMsg(online ? '✓ Satış kaydedildi — buluta gönderiliyor.' : '✓ Çevrimdışı kaydedildi — bağlantı gelince gönderilecek.');
      await onCommitted();
    } catch (e) {
      setMsg(`Kaydedilemedi: ${(e as Error).message ?? e}`);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="panel">
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <h2 style={{ margin: 0 }}>Yeni Satış</h2>
        <button onClick={() => void submit()} disabled={!canSubmit}>
          {busy ? 'Kaydediliyor…' : 'Kaydet'} <span style={{ opacity: 0.75, fontSize: 12 }}>(Ctrl+Enter)</span>
        </button>
      </div>

      <div className="xg-form" style={{ marginTop: 14 }}>
        <div className="xg-head">
          <div>
            <label>Alıcı</label>
            <select value={partyId} onChange={(e) => setPartyId(e.target.value)}>
              <option value="">— seçin —</option>
              {buyers.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
            </select>
          </div>
          <div>
            <label>Müstahsil</label>
            <select value={producerId} onChange={(e) => setProducerId(e.target.value)}>
              <option value="">— seçin —</option>
              {producers.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
          </div>
          <div>
            <label>Vade</label>
            <select value={saleTerm} onChange={(e) => setSaleTerm(Number(e.target.value) as SaleTerm)}>
              <option value={1}>Peşin</option>
              <option value={2}>Vadeli</option>
            </select>
          </div>
          <div>
            <label>Satış Tipi</label>
            <select value={isWithinMarket ? '1' : '0'} onChange={(e) => setIsWithinMarket(e.target.value === '1')}>
              <option value="1">Hal içi</option>
              <option value="0">Hal dışı</option>
            </select>
          </div>
        </div>

        <div className="xg-hint">
          <span><kbd>↑</kbd><kbd>↓</kbd> gez</span>
          <span><kbd>Enter</kbd> alt satır</span>
          <span><kbd>Tab</kbd> yatay</span>
          <span><kbd>F2</kbd> yeni satır</span>
          <span><kbd>Ctrl</kbd>+<kbd>Enter</kbd> kaydet</span>
        </div>

        <div className="xg-wrap">
          <table className="xg">
            <thead>
              <tr>
                <th className="xg-rownum">#</th>
                <th style={{ width: '38%' }}>Ürün / Cins</th>
                <th style={{ width: '15%' }}>Miktar</th>
                <th style={{ width: '15%' }}>Birim</th>
                <th style={{ width: '16%' }}>Fiyat</th>
                <th style={{ width: '16%' }}>Tutar</th>
                <th style={{ width: 36 }} />
              </tr>
            </thead>
            <tbody>
              {rows.map((row, i) => (
                <tr key={i}>
                  <td className="xg-rownum">{i + 1}</td>
                  <td>
                    <input ref={setRef(i, 0)} className="xg-cell" list="xg-products" value={row.productName}
                      placeholder="ürün yaz…" onChange={(e) => onProductInput(i, e.target.value)} onKeyDown={(e) => onKeyDown(e, i, 0)} />
                  </td>
                  <td>
                    <input ref={setRef(i, 1)} className="xg-cell xg-cell--num" inputMode="decimal" value={row.quantity}
                      onChange={(e) => update(i, { quantity: e.target.value })} onKeyDown={(e) => onKeyDown(e, i, 1)} />
                  </td>
                  <td>
                    <select ref={setRef(i, 2)} className="xg-cell" value={row.unitCode}
                      onChange={(e) => update(i, { unitCode: Number(e.target.value) as UnitOfMeasure })} onKeyDown={(e) => onKeyDown(e, i, 2)}>
                      {Object.entries(UNIT_LABELS).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
                    </select>
                  </td>
                  <td>
                    <input ref={setRef(i, 3)} className="xg-cell xg-cell--num" inputMode="decimal" value={row.unitPrice}
                      onChange={(e) => update(i, { unitPrice: e.target.value })} onKeyDown={(e) => onKeyDown(e, i, 3)} />
                  </td>
                  <td><div className="xg-ro">{formatTRY(calc.lineTotals[i] || 0)}</div></td>
                  <td><button className="xg-del" tabIndex={-1} onClick={() => removeRow(i)} aria-label="Sil">✕</button></td>
                </tr>
              ))}
            </tbody>
          </table>
          <datalist id="xg-products">
            {products.map((p) => <option key={p.id} value={p.name} />)}
          </datalist>
        </div>

        <button className="ghost" onClick={addRow} style={{ alignSelf: 'flex-start' }}>+ Satır ekle (F2)</button>

        <div className="xg-totals">
          <div className="xg-total"><p className="xg-total__l">Brüt</p><p className="xg-total__v">{formatTRY(calc.brut)}</p></div>
          <div className="xg-total"><p className="xg-total__l">Komisyon %8</p><p className="xg-total__v">{formatTRY(calc.komisyon)}</p></div>
          <div className="xg-total xg-total--net"><p className="xg-total__l">Müstahsil Net (tahmini)</p><p className="xg-total__v">{formatTRY(calc.net)}</p></div>
        </div>

        {msg && <p className="muted">{msg}</p>}
        <p className="muted">Kesin komisyon/stopaj/rüsum sync sonrası sunucuda hesaplanır (kaynak-doğruluk backend).</p>
      </div>
    </section>
  );
}

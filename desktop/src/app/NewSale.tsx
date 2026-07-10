import { useMemo, useState } from 'react';
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

interface Props {
  db: LocalDb | null;
  products: CachedProduct[];
  parties: CachedParty[];
  online: boolean;
  onCommitted: () => void | Promise<void>;
}

interface DraftLine {
  productId: string;
  quantity: string;
  unitCode: UnitOfMeasure;
  unitPrice: string;
}

function emptyLine(): DraftLine {
  return { productId: '', quantity: '', unitCode: 1, unitPrice: '' };
}

export function NewSale({ db, products, parties, online, onCommitted }: Props) {
  const buyers = useMemo(() => parties.filter((p) => p.partyType === 2), [parties]);
  const [partyId, setPartyId] = useState('');
  const [saleTerm, setSaleTerm] = useState<SaleTerm>(1);
  const [isWithinMarket, setIsWithinMarket] = useState(true);
  const [lines, setLines] = useState<DraftLine[]>([emptyLine()]);
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  const numericLines: SaleLineInput[] = lines
    .filter((l) => l.productId && Number(l.quantity) > 0 && Number(l.unitPrice) > 0)
    .map((l) => {
      const product = products.find((p) => p.id === l.productId);
      return {
        productId: l.productId,
        productName: product?.name ?? '',
        quantity: Number(l.quantity),
        unitCode: l.unitCode,
        unitPrice: Number(l.unitPrice),
      };
    });

  const total = grossTotal(numericLines);
  const canSubmit = !!db && !!partyId && numericLines.length > 0 && !busy;

  function updateLine(i: number, patch: Partial<DraftLine>) {
    setLines((prev) => prev.map((l, idx) => (idx === i ? { ...l, ...patch } : l)));
  }
  function addLine() {
    setLines((prev) => [...prev, emptyLine()]);
  }
  function removeLine(i: number) {
    setLines((prev) => (prev.length === 1 ? prev : prev.filter((_, idx) => idx !== i)));
  }

  async function submit() {
    if (!db || !canSubmit) return;
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
          saleTerm,
          isWithinMarket,
          lines: numericLines,
          grossTotal: total,
          status: 'completed',
          syncStatus: 'pending',
          createdAt,
        },
        seq,
      );
      setLines([emptyLine()]);
      setPartyId('');
      setMsg(
        online
          ? 'Satış kaydedildi — buluta gönderiliyor.'
          : 'Satış çevrimdışı kaydedildi — bağlantı gelince gönderilecek.',
      );
      await onCommitted();
    } catch (e) {
      setMsg(`Kaydedilemedi: ${(e as Error).message ?? e}`);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="panel">
      <h2>Yeni Satış</h2>

      <div className="row">
        <div>
          <label>Alıcı</label>
          <select value={partyId} onChange={(e) => setPartyId(e.target.value)}>
            <option value="">— seçin —</option>
            {buyers.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name}
              </option>
            ))}
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
          <label>Hal içi/dışı</label>
          <select
            value={isWithinMarket ? '1' : '0'}
            onChange={(e) => setIsWithinMarket(e.target.value === '1')}
          >
            <option value="1">Hal içi</option>
            <option value="0">Hal dışı</option>
          </select>
        </div>
      </div>

      {lines.map((line, i) => (
        <div className="line-item" key={i}>
          <div style={{ flex: 2 }}>
            <label>Ürün</label>
            <select
              value={line.productId}
              onChange={(e) => {
                const prod = products.find((p) => p.id === e.target.value);
                updateLine(i, { productId: e.target.value, unitCode: prod?.defaultUnit ?? 1 });
              }}
            >
              <option value="">— ürün —</option>
              {products.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label>Miktar</label>
            <input
              type="number"
              min="0"
              step="0.01"
              value={line.quantity}
              onChange={(e) => updateLine(i, { quantity: e.target.value })}
            />
          </div>
          <div>
            <label>Birim</label>
            <select
              value={line.unitCode}
              onChange={(e) => updateLine(i, { unitCode: Number(e.target.value) as UnitOfMeasure })}
            >
              {Object.entries(UNIT_LABELS).map(([k, v]) => (
                <option key={k} value={k}>
                  {v}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label>Birim Fiyat</label>
            <input
              type="number"
              min="0"
              step="0.01"
              value={line.unitPrice}
              onChange={(e) => updateLine(i, { unitPrice: e.target.value })}
            />
          </div>
          <div style={{ flex: '0 0 auto' }}>
            <label>&nbsp;</label>
            <button className="ghost" onClick={() => removeLine(i)} disabled={lines.length === 1}>
              ✕
            </button>
          </div>
        </div>
      ))}

      <button className="ghost" onClick={addLine} style={{ marginBottom: 8 }}>
        + Satır Ekle
      </button>

      <div className="total">Toplam: {formatTRY(total)}</div>
      <p className="muted">
        Gösterilen tutar tahminidir; komisyon/stopaj/rüsum kesintileri sync sonrası sunucuda
        hesaplanır (kaynak-doğruluk backend).
      </p>

      <button onClick={submit} disabled={!canSubmit} style={{ width: '100%' }}>
        {busy ? 'Kaydediliyor…' : 'Satışı Kaydet'}
      </button>
      {msg && <p className="muted" style={{ marginTop: 10 }}>{msg}</p>}
    </section>
  );
}

'use client';

// Yeni Mal Geliş — müstahsilden gelen mal partisini kabul eder (docs/03 M3). Müstahsil + tarih
// + sevk irsaliye referansı + bir/çok kalem (ürün/miktar/birim). POST /api/sales/consignments;
// ConsignmentReceived yayılır → Inventory stok girişi + Integration künye (ProductPassport).
// NOT: Ürün kataloğu servisi yok → ürün ID (GUID) elle girilir (MVP).

import { useRouter } from 'next/navigation';
import { useState, type FormEvent } from 'react';

import { usePartyOptions } from '@/features/parties/use-party-options';
import { useProductOptions } from '@/features/products/use-product-options';
import {
  receiveConsignment,
  UNIT_LABEL,
  UnitOfMeasure,
  type ConsignmentItemInput,
} from '@/features/sales/sales-api';
import { isApiError } from '@/lib/api-client';

interface ItemRow {
  productId: string;
  quantity: string;
  unit: number;
}

function emptyRow(): ItemRow {
  return { productId: '', quantity: '', unit: UnitOfMeasure.Crate };
}

function todayInput(): string {
  return new Date().toISOString().slice(0, 10);
}

export default function NewConsignmentPage() {
  const router = useRouter();
  const { options, loading: partiesLoading } = usePartyOptions();
  const { options: products, loading: productsLoading } = useProductOptions();

  const [producerPartyId, setProducerPartyId] = useState('');
  const [receivedAt, setReceivedAt] = useState(todayInput());
  const [dispatchNoteRef, setDispatchNoteRef] = useState('');
  const [rows, setRows] = useState<ItemRow[]>([emptyRow()]);

  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function updateRow(index: number, patch: Partial<ItemRow>) {
    setRows((prev) => prev.map((r, i) => (i === index ? { ...r, ...patch } : r)));
  }
  function addRow() {
    setRows((prev) => [...prev, emptyRow()]);
  }
  function removeRow(index: number) {
    setRows((prev) => prev.filter((_, i) => i !== index));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (!producerPartyId) {
      setError('Müstahsil seçilmelidir.');
      return;
    }

    const items: ConsignmentItemInput[] = [];
    for (const [i, r] of rows.entries()) {
      const quantity = Number(r.quantity);
      if (!r.productId.trim()) {
        setError(`${i + 1}. kalem: ürün ID zorunlu.`);
        return;
      }
      if (!Number.isFinite(quantity) || quantity <= 0) {
        setError(`${i + 1}. kalem: miktar sıfırdan büyük olmalı.`);
        return;
      }
      items.push({ productId: r.productId.trim(), quantity, unit: r.unit });
    }

    setSubmitting(true);
    try {
      await receiveConsignment({
        producerPartyId,
        receivedAt: new Date(receivedAt).toISOString(),
        dispatchNoteRef: dispatchNoteRef.trim() || null,
        items,
      });
      router.push('/dashboard/satis');
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Mal geliş kaydedilemedi.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="form-page form-page--wide">
      <h1 className="page-title">Yeni Mal Geliş</h1>

      <form className="form-card" onSubmit={handleSubmit}>
        {error ? <div className="form-error">{error}</div> : null}

        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="producer">Müstahsil *</label>
            <select
              id="producer"
              value={producerPartyId}
              onChange={(e) => setProducerPartyId(e.target.value)}
              required
              disabled={partiesLoading}
            >
              <option value="">{partiesLoading ? 'Yükleniyor…' : 'Seçiniz…'}</option>
              {options.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.displayName}
                </option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="receivedAt">Geliş Tarihi *</label>
            <input
              id="receivedAt"
              type="date"
              value={receivedAt}
              onChange={(e) => setReceivedAt(e.target.value)}
              required
            />
          </div>
          <div className="form-field">
            <label htmlFor="dispatchNoteRef">Sevk İrsaliye No</label>
            <input
              id="dispatchNoteRef"
              value={dispatchNoteRef}
              onChange={(e) => setDispatchNoteRef(e.target.value)}
              placeholder="opsiyonel"
            />
          </div>
        </div>

        <fieldset className="form-fieldset">
          <legend>Kalemler</legend>
          <table className="line-table">
            <thead>
              <tr>
                <th>Ürün</th>
                <th className="data-table__num">Miktar</th>
                <th>Birim</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {rows.map((r, i) => (
                <tr key={i}>
                  <td>
                    <select
                      value={r.productId}
                      disabled={productsLoading}
                      onChange={(e) => {
                        const opt = products.find((p) => p.id === e.target.value);
                        updateRow(
                          i,
                          opt
                            ? { productId: opt.id, unit: opt.defaultUnit }
                            : { productId: e.target.value },
                        );
                      }}
                    >
                      <option value="">
                        {productsLoading ? 'Yükleniyor…' : 'Ürün seçin…'}
                      </option>
                      {products.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.name}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <input
                      type="number"
                      step="0.001"
                      min="0"
                      className="input-num"
                      value={r.quantity}
                      onChange={(e) => updateRow(i, { quantity: e.target.value })}
                    />
                  </td>
                  <td>
                    <select
                      value={r.unit}
                      onChange={(e) => updateRow(i, { unit: Number(e.target.value) })}
                    >
                      {Object.entries(UNIT_LABEL).map(([value, name]) => (
                        <option key={value} value={value}>
                          {name}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <button
                      type="button"
                      className="btn-icon"
                      onClick={() => removeRow(i)}
                      disabled={rows.length <= 1}
                      aria-label="Kalemi sil"
                    >
                      ✕
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <button type="button" className="btn-secondary btn-sm" onClick={addRow}>
            + Kalem ekle
          </button>
        </fieldset>

        <div className="form-actions">
          <button
            type="button"
            className="btn-secondary"
            onClick={() => router.push('/dashboard/satis')}
            disabled={submitting}
          >
            İptal
          </button>
          <button
            type="submit"
            className="btn-primary btn-inline"
            disabled={submitting}
          >
            {submitting ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </div>
      </form>
    </div>
  );
}

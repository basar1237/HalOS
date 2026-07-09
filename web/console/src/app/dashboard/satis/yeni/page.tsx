'use client';

// Yeni Satış — başlık (alıcı/müstahsil/tarih/vade/hal-içi) + bir veya daha çok satır
// (ürün/miktar/birim/birim fiyat). Kaydet: oluştur → satırları ekle → tamamla (kesinti/hakediş
// motoru, SaleCompleted). NOT: Ürün kataloğu servisi yok → ürün ID (GUID) elle girilir (MVP).

import { useRouter } from 'next/navigation';
import { useState, type FormEvent } from 'react';

import { usePartyOptions } from '@/features/parties/use-party-options';
import { useProductOptions } from '@/features/products/use-product-options';
import {
  createCompleteSale,
  SaleTerm,
  UNIT_LABEL,
  UnitOfMeasure,
  type SaleLineInput,
} from '@/features/sales/sales-api';
import { isApiError } from '@/lib/api-client';

interface LineRow {
  productId: string;
  quantity: string;
  unit: number;
  unitPrice: string;
}

function emptyRow(): LineRow {
  return { productId: '', quantity: '', unit: UnitOfMeasure.Crate, unitPrice: '' };
}

function todayInput(): string {
  return new Date().toISOString().slice(0, 10);
}

export default function NewSalePage() {
  const router = useRouter();
  const { options, loading: partiesLoading } = usePartyOptions();
  const { options: products, loading: productsLoading } = useProductOptions();

  const [buyerPartyId, setBuyerPartyId] = useState('');
  const [producerPartyId, setProducerPartyId] = useState('');
  const [soldAt, setSoldAt] = useState(todayInput());
  const [term, setTerm] = useState<number>(SaleTerm.Cash);
  const [isWithinMarket, setIsWithinMarket] = useState(true);
  const [rows, setRows] = useState<LineRow[]>([emptyRow()]);

  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function updateRow(index: number, patch: Partial<LineRow>) {
    setRows((prev) =>
      prev.map((r, i) => (i === index ? { ...r, ...patch } : r)),
    );
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

    if (!buyerPartyId || !producerPartyId) {
      setError('Alıcı ve müstahsil seçilmelidir.');
      return;
    }

    // Satırları doğrula + dönüştür.
    const lines: SaleLineInput[] = [];
    for (const [i, r] of rows.entries()) {
      const quantity = Number(r.quantity);
      const unitPrice = Number(r.unitPrice);
      if (!r.productId.trim()) {
        setError(`${i + 1}. satır: ürün ID zorunlu.`);
        return;
      }
      if (!Number.isFinite(quantity) || quantity <= 0) {
        setError(`${i + 1}. satır: miktar sıfırdan büyük olmalı.`);
        return;
      }
      if (!Number.isFinite(unitPrice) || unitPrice <= 0) {
        setError(`${i + 1}. satır: birim fiyat sıfırdan büyük olmalı.`);
        return;
      }
      lines.push({
        productId: r.productId.trim(),
        quantity,
        unit: r.unit,
        unitPrice,
      });
    }

    setSubmitting(true);
    try {
      await createCompleteSale(
        {
          buyerPartyId,
          producerPartyId,
          soldAt: new Date(soldAt).toISOString(),
          isWithinMarket,
          term,
        },
        lines,
      );
      router.push('/dashboard/satis');
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Satış kaydedilemedi.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="form-page form-page--wide">
      <h1 className="page-title">Yeni Satış</h1>

      <form className="form-card" onSubmit={handleSubmit}>
        {error ? <div className="form-error">{error}</div> : null}

        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="buyer">Alıcı *</label>
            <select
              id="buyer"
              value={buyerPartyId}
              onChange={(e) => setBuyerPartyId(e.target.value)}
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
            <label htmlFor="soldAt">Tarih *</label>
            <input
              id="soldAt"
              type="date"
              value={soldAt}
              onChange={(e) => setSoldAt(e.target.value)}
              required
            />
          </div>
          <div className="form-field">
            <label htmlFor="term">Vade *</label>
            <select
              id="term"
              value={term}
              onChange={(e) => setTerm(Number(e.target.value))}
            >
              <option value={SaleTerm.Cash}>Peşin (15 iş günü)</option>
              <option value={SaleTerm.Deferred}>Vadeli (30 gün)</option>
            </select>
          </div>
        </div>

        <label className="checkbox">
          <input
            type="checkbox"
            checked={isWithinMarket}
            onChange={(e) => setIsWithinMarket(e.target.checked)}
          />
          Hal içi satış
        </label>

        <fieldset className="form-fieldset">
          <legend>Satırlar</legend>
          <table className="line-table">
            <thead>
              <tr>
                <th>Ürün</th>
                <th className="data-table__num">Miktar</th>
                <th>Birim</th>
                <th className="data-table__num">Birim Fiyat</th>
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
                        // Ürün seçilince satır birimini ürünün varsayılan birimine ayarla.
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
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      className="input-num"
                      value={r.unitPrice}
                      onChange={(e) => updateRow(i, { unitPrice: e.target.value })}
                    />
                  </td>
                  <td>
                    <button
                      type="button"
                      className="btn-icon"
                      onClick={() => removeRow(i)}
                      disabled={rows.length <= 1}
                      aria-label="Satırı sil"
                    >
                      ✕
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <button type="button" className="btn-secondary btn-sm" onClick={addRow}>
            + Satır ekle
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
            {submitting ? 'Kaydediliyor…' : 'Kaydet ve Tamamla'}
          </button>
        </div>
      </form>
    </div>
  );
}

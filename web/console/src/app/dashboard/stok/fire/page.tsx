'use client';

// Fire (zayiat) kaydı — docs/03 M9 / BK-7. Ürün seçici + miktar + gerekçe + tarih. Fire mevcut
// stoğu aşamaz (BK-7); backend reddederse ProblemDetails mesajı gösterilir. Kayıt stok çıkışı üretir.

import { useRouter } from 'next/navigation';
import { useState, type FormEvent } from 'react';

import { useProductOptions } from '@/features/products/use-product-options';
import { recordSpoilage } from '@/features/inventory/inventory-api';
import { isApiError } from '@/lib/api-client';

function todayInput(): string {
  return new Date().toISOString().slice(0, 10);
}

export default function SpoilagePage() {
  const router = useRouter();
  const { options: products, loading: productsLoading } = useProductOptions();

  const [productId, setProductId] = useState('');
  const [quantity, setQuantity] = useState('');
  const [reason, setReason] = useState('');
  const [occurredAt, setOccurredAt] = useState(todayInput());
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    const quantityNum = Number(quantity);
    if (!productId) {
      setError('Ürün seçilmelidir.');
      return;
    }
    if (!Number.isFinite(quantityNum) || quantityNum <= 0) {
      setError('Miktar sıfırdan büyük olmalıdır.');
      return;
    }
    if (!reason.trim()) {
      setError('Fire gerekçesi zorunludur.');
      return;
    }

    setSubmitting(true);
    try {
      await recordSpoilage({
        productId,
        quantity: quantityNum,
        reason: reason.trim(),
        occurredAt: new Date(occurredAt).toISOString(),
      });
      router.push('/dashboard/stok');
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Fire kaydedilemedi.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="form-page">
      <h1 className="page-title">Fire Kaydı</h1>

      <form className="form-card" onSubmit={handleSubmit}>
        {error ? <div className="form-error">{error}</div> : null}

        <div className="form-field">
          <label htmlFor="product">Ürün *</label>
          <select
            id="product"
            value={productId}
            onChange={(e) => setProductId(e.target.value)}
            required
            disabled={productsLoading}
          >
            <option value="">{productsLoading ? 'Yükleniyor…' : 'Seçiniz…'}</option>
            {products.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </div>

        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="quantity">Miktar *</label>
            <input
              id="quantity"
              type="number"
              step="0.001"
              min="0"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              required
            />
          </div>
          <div className="form-field">
            <label htmlFor="occurredAt">Tarih *</label>
            <input
              id="occurredAt"
              type="date"
              value={occurredAt}
              onChange={(e) => setOccurredAt(e.target.value)}
              required
            />
          </div>
        </div>

        <div className="form-field">
          <label htmlFor="reason">Gerekçe *</label>
          <input
            id="reason"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            required
            placeholder="ör. taşımada ezilme"
          />
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn-secondary"
            onClick={() => router.push('/dashboard/stok')}
            disabled={submitting}
          >
            İptal
          </button>
          <button type="submit" className="btn-primary btn-inline" disabled={submitting}>
            {submitting ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </div>
      </form>
    </div>
  );
}

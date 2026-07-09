'use client';

// Yeni Ürün — ürün kataloğuna kayıt (POST /api/inventory/products, docs/03 M2). Ad + kategori(ops.)
// + varsayılan birim. Yetki: Patron/Yönetici (backend zorlar; ProblemDetails mesajı yansır).

import { useRouter } from 'next/navigation';
import { useState, type FormEvent } from 'react';

import { createProduct } from '@/features/products/product-api';
import { UNIT_LABEL, UnitOfMeasure } from '@/features/sales/sales-api';
import { isApiError } from '@/lib/api-client';

export default function NewProductPage() {
  const router = useRouter();

  const [name, setName] = useState('');
  const [category, setCategory] = useState('');
  const [defaultUnit, setDefaultUnit] = useState<number>(UnitOfMeasure.Crate);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await createProduct({
        name: name.trim(),
        category: category.trim() || null,
        defaultUnit,
      });
      router.push('/dashboard/stok/urunler');
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Ürün oluşturulamadı.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="form-page">
      <h1 className="page-title">Yeni Ürün</h1>

      <form className="form-card" onSubmit={handleSubmit}>
        {error ? <div className="form-error">{error}</div> : null}

        <div className="form-field">
          <label htmlFor="name">Ad *</label>
          <input
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            maxLength={200}
          />
        </div>

        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="category">Kategori</label>
            <input
              id="category"
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              maxLength={100}
              placeholder="ör. Sebze"
            />
          </div>
          <div className="form-field">
            <label htmlFor="defaultUnit">Varsayılan Birim *</label>
            <select
              id="defaultUnit"
              value={defaultUnit}
              onChange={(e) => setDefaultUnit(Number(e.target.value))}
            >
              {Object.entries(UNIT_LABEL).map(([value, unitName]) => (
                <option key={value} value={value}>
                  {unitName}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn-secondary"
            onClick={() => router.push('/dashboard/stok/urunler')}
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

'use client';

// Yeni soğuk oda tanımlama — docs/04 §6 / S3.1. Ad + izin verilen sıcaklık aralığı (alt < üst).
// POST /api/coldchain/cold-storage-units. Backend alt < üst değişmezini zorlar (422 → mesaj gösterilir).

import { useRouter } from 'next/navigation';
import { useState, type FormEvent } from 'react';

import { registerColdStorageUnit } from '@/features/coldchain/coldchain-api';
import { isApiError } from '@/lib/api-client';

export default function NewColdStorageUnitPage() {
  const router = useRouter();

  const [name, setName] = useState('');
  const [minTempC, setMinTempC] = useState('0');
  const [maxTempC, setMaxTempC] = useState('4');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    const min = Number(minTempC);
    const max = Number(maxTempC);
    if (!name.trim()) {
      setError('Soğuk oda adı zorunludur.');
      return;
    }
    if (!Number.isFinite(min) || !Number.isFinite(max)) {
      setError('Sıcaklık eşikleri geçerli sayı olmalıdır.');
      return;
    }
    if (min >= max) {
      setError('Alt sıcaklık eşiği üst eşikten küçük olmalıdır.');
      return;
    }

    setSubmitting(true);
    try {
      await registerColdStorageUnit({ name: name.trim(), minTempC: min, maxTempC: max });
      router.push('/dashboard/soguk-zincir');
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Soğuk oda kaydedilemedi.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="form-page">
      <h1 className="page-title">Yeni Soğuk Oda</h1>

      <form className="form-card" onSubmit={handleSubmit}>
        {error ? <div className="form-error">{error}</div> : null}

        <div className="form-field">
          <label htmlFor="name">Ad *</label>
          <input
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            placeholder="ör. 1 No'lu Soğuk Oda"
          />
        </div>

        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="minTempC">Alt Eşik (°C) *</label>
            <input
              id="minTempC"
              type="number"
              step="0.1"
              value={minTempC}
              onChange={(e) => setMinTempC(e.target.value)}
              required
            />
          </div>
          <div className="form-field">
            <label htmlFor="maxTempC">Üst Eşik (°C) *</label>
            <input
              id="maxTempC"
              type="number"
              step="0.1"
              value={maxTempC}
              onChange={(e) => setMaxTempC(e.target.value)}
              required
            />
          </div>
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn-secondary"
            onClick={() => router.push('/dashboard/soguk-zincir')}
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

'use client';

// Soğuk oda detayı — bilgi + eşik güncelleme + son sensör okumaları (docs/04 §6, S3.1).
// GET /api/coldchain/cold-storage-units/{id} (+ /readings); PUT .../thresholds.

import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useCallback, useEffect, useState, type FormEvent } from 'react';

import {
  getColdStorageUnit,
  isBreaching,
  listReadings,
  updateThresholds,
  type ColdStorageUnit,
  type SensorReading,
} from '@/features/coldchain/coldchain-api';
import { isApiError } from '@/lib/api-client';

const TEMP = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 1 });
const DATETIME = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'short' });

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : DATETIME.format(d);
}

export default function ColdStorageUnitDetailPage() {
  const params = useParams<{ id: string }>();
  const id = params.id;
  const router = useRouter();

  const [unit, setUnit] = useState<ColdStorageUnit | null>(null);
  const [readings, setReadings] = useState<SensorReading[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [minTempC, setMinTempC] = useState('');
  const [maxTempC, setMaxTempC] = useState('');
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [u, r] = await Promise.all([getColdStorageUnit(id), listReadings(id, 50)]);
      setUnit(u);
      setReadings(r);
      setMinTempC(String(u.minTempC));
      setMaxTempC(String(u.maxTempC));
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Soğuk oda yüklenemedi.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleThresholdSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaveError(null);

    const min = Number(minTempC);
    const max = Number(maxTempC);
    if (!Number.isFinite(min) || !Number.isFinite(max) || min >= max) {
      setSaveError('Alt sıcaklık eşiği üst eşikten küçük olmalıdır.');
      return;
    }

    setSaving(true);
    try {
      await updateThresholds(id, { minTempC: min, maxTempC: max });
      await load();
    } catch (err) {
      setSaveError(isApiError(err) ? err.message : 'Eşik güncellenemedi.');
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <div className="page-title">Yükleniyor…</div>;
  if (error) return <div className="form-error">{error}</div>;
  if (!unit) return <div className="form-error">Soğuk oda bulunamadı.</div>;

  const breaching = isBreaching(unit);

  return (
    <div>
      <div className="page-head">
        <h1 className="page-title">{unit.name}</h1>
        <div className="btn-group">
          <Link href="/dashboard/soguk-zincir" className="btn-secondary btn-sm">
            ← Soğuk Zincir
          </Link>
        </div>
      </div>

      <div className="form-card">
        <p>
          Son sıcaklık:{' '}
          <strong>
            {unit.latestTemperatureC == null
              ? '—'
              : `${TEMP.format(unit.latestTemperatureC)} °C`}
          </strong>{' '}
          {unit.latestTemperatureC != null &&
            (breaching ? (
              <span className="badge badge--warn">Alarm</span>
            ) : (
              <span className="badge">Normal</span>
            ))}
        </p>
        <p className="muted">
          {unit.latestReadingAt ? `Son okuma: ${formatDate(unit.latestReadingAt)}` : 'Henüz okuma yok'}
          {' · '}
          Toplam {unit.readingCount} okuma
          {' · '}
          {unit.isActive ? 'Aktif' : 'Pasif'}
        </p>
      </div>

      <form className="form-card" onSubmit={handleThresholdSubmit}>
        <h2 className="section-title">İzin Verilen Sıcaklık Aralığı</h2>
        {saveError ? <div className="form-error">{saveError}</div> : null}
        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="minTempC">Alt Eşik (°C)</label>
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
            <label htmlFor="maxTempC">Üst Eşik (°C)</label>
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
          <button type="submit" className="btn-primary btn-inline" disabled={saving}>
            {saving ? 'Kaydediliyor…' : 'Eşikleri Güncelle'}
          </button>
        </div>
      </form>

      <div className="form-card">
        <h2 className="section-title">Son Okumalar</h2>
        {readings.length === 0 ? (
          <p className="muted">Henüz okuma yok.</p>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Zaman</th>
                <th className="data-table__num">Sıcaklık</th>
                <th className="data-table__num">Nem</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {readings.map((r) => {
                const out = r.temperatureC > unit.maxTempC || r.temperatureC < unit.minTempC;
                return (
                  <tr key={r.id}>
                    <td>{formatDate(r.occurredAt)}</td>
                    <td className="data-table__num">{TEMP.format(r.temperatureC)} °C</td>
                    <td className="data-table__num">
                      {r.humidityPercent == null ? '—' : `%${TEMP.format(r.humidityPercent)}`}
                    </td>
                    <td>
                      {out ? <span className="badge badge--warn">Eşik dışı</span> : 'Normal'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

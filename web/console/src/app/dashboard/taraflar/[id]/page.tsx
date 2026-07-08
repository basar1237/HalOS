'use client';

// Taraf detay + düzenleme — GET/PUT/DELETE /api/party/parties/{id}. Roller ve TCKN/VKN salt
// okunur (oluşturmada belirlenir); ad/iletişim/defter/stopaj düzenlenebilir. Pasifleştir soft-delete.

import { useParams, useRouter } from 'next/navigation';
import { useCallback, useEffect, useState, type FormEvent } from 'react';

import { label, PARTY_ROLE_LABEL } from '@/features/common/labels';
import {
  deactivateParty,
  getParty,
  updateParty,
} from '@/features/parties/party-api';
import { isApiError } from '@/lib/api-client';
import type { Party } from '@/shared/entities';

const PRODUCER_ROLE = 1;

function nullIfBlank(value: string): string | null {
  const t = value.trim();
  return t === '' ? null : t;
}

export default function PartyDetailPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const id = params.id;

  const [party, setParty] = useState<Party | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  // Düzenlenebilir alanlar
  const [displayName, setDisplayName] = useState('');
  const [taxOffice, setTaxOffice] = useState('');
  const [phone, setPhone] = useState('');
  const [address, setAddress] = useState('');
  const [keepsRecords, setKeepsRecords] = useState(false);
  const [agriRate, setAgriRate] = useState('0.02');
  const [sskRate, setSskRate] = useState('0.01');

  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [deactivating, setDeactivating] = useState(false);

  const isProducer = party?.roles.includes(PRODUCER_ROLE) ?? false;

  const hydrate = useCallback((p: Party) => {
    setParty(p);
    setDisplayName(p.displayName);
    setTaxOffice(p.taxOffice ?? '');
    setPhone(p.phone ?? '');
    setAddress(p.address ?? '');
    setKeepsRecords(p.keepsRecords);
    if (p.withholdingProfile) {
      setAgriRate(String(p.withholdingProfile.agriWithholdingRate));
      setSskRate(String(p.withholdingProfile.farmerSskRate));
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    getParty(id)
      .then((p) => {
        if (!cancelled) hydrate(p);
      })
      .catch((err) => {
        if (!cancelled)
          setLoadError(isApiError(err) ? err.message : 'Taraf yüklenemedi.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [id, hydrate]);

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSaving(true);
    try {
      await updateParty(id, {
        displayName: displayName.trim(),
        taxOffice: nullIfBlank(taxOffice),
        phone: nullIfBlank(phone),
        address: nullIfBlank(address),
        keepsRecords,
        withholdingProfile: isProducer
          ? {
              agriWithholdingRate: Number(agriRate),
              farmerSskRate: Number(sskRate),
            }
          : null,
      });
      router.push('/dashboard/taraflar');
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Kaydedilemedi.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDeactivate() {
    if (!window.confirm('Bu taraf pasifleştirilecek. Onaylıyor musunuz?')) return;
    setError(null);
    setDeactivating(true);
    try {
      await deactivateParty(id);
      router.push('/dashboard/taraflar');
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Pasifleştirilemedi.');
      setDeactivating(false);
    }
  }

  if (loading) return <p className="page-state">Yükleniyor…</p>;
  if (loadError)
    return <p className="page-state page-state--error">{loadError}</p>;
  if (!party) return <p className="page-state">Taraf bulunamadı.</p>;

  return (
    <div className="form-page">
      <div className="page-head">
        <h1 className="page-title">{party.displayName}</h1>
        <span className={party.isActive ? 'badge badge--ok' : 'badge'}>
          {party.isActive ? 'Aktif' : 'Pasif'}
        </span>
      </div>

      <form className="form-card" onSubmit={handleSave}>
        {error ? <div className="form-error">{error}</div> : null}

        <div className="detail-meta">
          <span>
            <strong>Roller:</strong>{' '}
            {party.roles.map((r) => label(PARTY_ROLE_LABEL, r)).join(', ') || '—'}
          </span>
          <span>
            <strong>VKN/TCKN:</strong> {party.vkn ?? party.tckn ?? '—'}
          </span>
        </div>

        <div className="form-field">
          <label htmlFor="displayName">Ad *</label>
          <input
            id="displayName"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            required
            maxLength={200}
          />
        </div>

        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="taxOffice">Vergi Dairesi</label>
            <input
              id="taxOffice"
              value={taxOffice}
              onChange={(e) => setTaxOffice(e.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="phone">Telefon</label>
            <input
              id="phone"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
            />
          </div>
        </div>

        <div className="form-field">
          <label htmlFor="address">Adres</label>
          <input
            id="address"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
          />
        </div>

        <label className="checkbox">
          <input
            type="checkbox"
            checked={keepsRecords}
            onChange={(e) => setKeepsRecords(e.target.checked)}
          />
          Defter tutan (kayıt tutan) mükellef
        </label>

        {isProducer ? (
          <fieldset className="form-fieldset">
            <legend>Stopaj Profili (Müstahsil)</legend>
            <div className="form-grid">
              <div className="form-field">
                <label htmlFor="agriRate">Zirai Stopaj Oranı (0-1)</label>
                <input
                  id="agriRate"
                  type="number"
                  step="0.001"
                  min="0"
                  max="1"
                  value={agriRate}
                  onChange={(e) => setAgriRate(e.target.value)}
                />
              </div>
              <div className="form-field">
                <label htmlFor="sskRate">Çiftçi Bağ-Kur Oranı (0-1)</label>
                <input
                  id="sskRate"
                  type="number"
                  step="0.001"
                  min="0"
                  max="1"
                  value={sskRate}
                  onChange={(e) => setSskRate(e.target.value)}
                />
              </div>
            </div>
          </fieldset>
        ) : null}

        <div className="form-actions form-actions--split">
          <button
            type="button"
            className="btn-danger"
            onClick={handleDeactivate}
            disabled={saving || deactivating || !party.isActive}
          >
            {deactivating ? 'Pasifleştiriliyor…' : 'Pasifleştir'}
          </button>
          <div className="form-actions">
            <button
              type="button"
              className="btn-secondary"
              onClick={() => router.push('/dashboard/taraflar')}
              disabled={saving || deactivating}
            >
              Geri
            </button>
            <button
              type="submit"
              className="btn-primary btn-inline"
              disabled={saving || deactivating}
            >
              {saving ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        </div>
      </form>
    </div>
  );
}

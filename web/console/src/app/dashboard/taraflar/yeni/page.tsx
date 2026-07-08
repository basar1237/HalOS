'use client';

// Yeni Taraf formu — POST /api/party/parties (yazma: Patron/Yönetici). Producer (Müstahsil)
// rolü seçilince domain stopaj profilini zorunlu kılar; form bu alanları o zaman gösterir.
// Format doğrulaması (TCKN 11 / VKN 10) yüzeyseldir; asıl doğrulama backend'de (mesajı gösterilir).

import { useRouter } from 'next/navigation';
import { useMemo, useState, type FormEvent } from 'react';

import { PARTY_ROLE_LABEL } from '@/features/common/labels';
import { createParty } from '@/features/parties/create-party';
import { isApiError } from '@/lib/api-client';

const PRODUCER_ROLE = 1;
const ROLE_OPTIONS = [1, 2, 3, 4];

function nullIfBlank(value: string): string | null {
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}

export default function NewPartyPage() {
  const router = useRouter();

  const [displayName, setDisplayName] = useState('');
  const [tckn, setTckn] = useState('');
  const [vkn, setVkn] = useState('');
  const [taxOffice, setTaxOffice] = useState('');
  const [phone, setPhone] = useState('');
  const [address, setAddress] = useState('');
  const [keepsRecords, setKeepsRecords] = useState(false);
  const [roles, setRoles] = useState<number[]>([]);
  // Domain varsayılanları: zirai stopaj %2, çiftçi Bağ-Kur %1 (oran 0-1).
  const [agriRate, setAgriRate] = useState('0.02');
  const [sskRate, setSskRate] = useState('0.01');

  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const isProducer = useMemo(() => roles.includes(PRODUCER_ROLE), [roles]);

  function toggleRole(role: number) {
    setRoles((prev) =>
      prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role],
    );
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (roles.length === 0) {
      setError('En az bir rol seçilmelidir.');
      return;
    }

    setSubmitting(true);
    try {
      await createParty({
        displayName: displayName.trim(),
        tckn: nullIfBlank(tckn),
        vkn: nullIfBlank(vkn),
        taxOffice: nullIfBlank(taxOffice),
        phone: nullIfBlank(phone),
        address: nullIfBlank(address),
        keepsRecords,
        // Producer için stopaj profili gönder (domain zorunlu). Değilse null.
        withholdingProfile: isProducer
          ? {
              agriWithholdingRate: Number(agriRate),
              farmerSskRate: Number(sskRate),
            }
          : null,
        roles,
      });
      router.push('/dashboard/taraflar');
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Taraf oluşturulamadı.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="form-page">
      <h1 className="page-title">Yeni Taraf</h1>

      <form className="form-card" onSubmit={handleSubmit}>
        {error ? <div className="form-error">{error}</div> : null}

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

        <fieldset className="form-fieldset">
          <legend>Roller *</legend>
          <div className="checkbox-row">
            {ROLE_OPTIONS.map((role) => (
              <label key={role} className="checkbox">
                <input
                  type="checkbox"
                  checked={roles.includes(role)}
                  onChange={() => toggleRole(role)}
                />
                {PARTY_ROLE_LABEL[role]}
              </label>
            ))}
          </div>
        </fieldset>

        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="tckn">TCKN</label>
            <input
              id="tckn"
              value={tckn}
              onChange={(e) => setTckn(e.target.value)}
              inputMode="numeric"
              maxLength={11}
              placeholder="11 hane"
            />
          </div>
          <div className="form-field">
            <label htmlFor="vkn">VKN</label>
            <input
              id="vkn"
              value={vkn}
              onChange={(e) => setVkn(e.target.value)}
              inputMode="numeric"
              maxLength={10}
              placeholder="10 hane"
            />
          </div>
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
            <legend>Stopaj Profili (Müstahsil için zorunlu)</legend>
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

        <div className="form-actions">
          <button
            type="button"
            className="btn-secondary"
            onClick={() => router.push('/dashboard/taraflar')}
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

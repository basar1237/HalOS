'use client';

// Finans hareketi kaydı — ödeme/tahsilat/avans (docs/03 M6). İşlem türü ?tur= ile gelir.
// BK-6: 7.000 TL üstü nakit reddedilir → form nakitte eşik aşımında uyarır ve engeller
// (asıl doğrulama backend'de). Taraf açılır seçiciden; tutar/kanal/tarih girilir.

import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useMemo, useState, type FormEvent } from 'react';

import { usePartyOptions } from '@/features/parties/use-party-options';
import {
  isMovementKind,
  MOVEMENT_TITLE,
  PaymentChannel,
  recordMovement,
  type MovementKind,
} from '@/features/finance/finance-api';
import { isApiError } from '@/lib/api-client';

const CASH_LIMIT = 7000; // BK-6

function todayInput(): string {
  return new Date().toISOString().slice(0, 10);
}

function MovementForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const turParam = searchParams.get('tur');
  const kind: MovementKind = isMovementKind(turParam) ? turParam : 'payment';

  const { options, loading: partiesLoading } = usePartyOptions();

  const [partyId, setPartyId] = useState('');
  const [amount, setAmount] = useState('');
  const [channel, setChannel] = useState<number>(PaymentChannel.Cash);
  const [bankReference, setBankReference] = useState('');
  const [occurredAt, setOccurredAt] = useState(todayInput());
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const amountNum = Number(amount);
  const cashOverLimit =
    channel === PaymentChannel.Cash &&
    Number.isFinite(amountNum) &&
    amountNum > CASH_LIMIT;

  const partyLabel = useMemo(
    () => (kind === 'collection' ? 'Alıcı' : 'Müstahsil / Taraf'),
    [kind],
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (!partyId) {
      setError('Taraf seçilmelidir.');
      return;
    }
    if (!Number.isFinite(amountNum) || amountNum <= 0) {
      setError('Tutar sıfırdan büyük olmalıdır.');
      return;
    }
    if (cashOverLimit) {
      setError('7.000 TL üstü nakit yapılamaz (BK-6). Lütfen banka kanalını seçin.');
      return;
    }

    setSubmitting(true);
    try {
      await recordMovement(kind, {
        partyId,
        amount: amountNum,
        channel,
        bankReference:
          channel === PaymentChannel.Bank && bankReference.trim()
            ? bankReference.trim()
            : null,
        occurredAt: new Date(occurredAt).toISOString(),
      });
      router.push('/dashboard/finans');
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Kaydedilemedi.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="form-page">
      <h1 className="page-title">{MOVEMENT_TITLE[kind]}</h1>

      <form className="form-card" onSubmit={handleSubmit}>
        {error ? <div className="form-error">{error}</div> : null}

        <div className="form-field">
          <label htmlFor="partyId">{partyLabel} *</label>
          <select
            id="partyId"
            value={partyId}
            onChange={(e) => setPartyId(e.target.value)}
            required
            disabled={partiesLoading}
          >
            <option value="">
              {partiesLoading ? 'Yükleniyor…' : 'Seçiniz…'}
            </option>
            {options.map((o) => (
              <option key={o.id} value={o.id}>
                {o.displayName}
              </option>
            ))}
          </select>
        </div>

        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="amount">Tutar (TL) *</label>
            <input
              id="amount"
              type="number"
              step="0.01"
              min="0"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              required
            />
          </div>
          <div className="form-field">
            <label htmlFor="channel">Kanal *</label>
            <select
              id="channel"
              value={channel}
              onChange={(e) => setChannel(Number(e.target.value))}
            >
              <option value={PaymentChannel.Cash}>Nakit</option>
              <option value={PaymentChannel.Bank}>Banka</option>
            </select>
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

        {channel === PaymentChannel.Bank ? (
          <div className="form-field">
            <label htmlFor="bankReference">Banka Referansı</label>
            <input
              id="bankReference"
              value={bankReference}
              onChange={(e) => setBankReference(e.target.value)}
              placeholder="Dekont / referans no"
            />
          </div>
        ) : null}

        {cashOverLimit ? (
          <div className="form-warn">
            7.000 TL üstü nakit yapılamaz (BK-6). Banka kanalını seçin.
          </div>
        ) : null}

        <div className="form-actions">
          <button
            type="button"
            className="btn-secondary"
            onClick={() => router.push('/dashboard/finans')}
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

export default function MovementPage() {
  return (
    <Suspense fallback={<p className="page-state">Yükleniyor…</p>}>
      <MovementForm />
    </Suspense>
  );
}

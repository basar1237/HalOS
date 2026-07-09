'use client';

// Satış detayı — GET /api/sales/sales/{id}: başlık + satırlar + komisyon hesabı + kesintiler +
// hakediş. Tamamlanmış satış iptal edilebilir (POST /cancel, ters kayıt/flag; SİLİNMEZ — BK-9,
// yetki Patron/Yönetici). Ürün adları katalog seçicisinden çözülür.

import { useParams, useRouter } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';

import { label } from '@/features/common/labels';
import { useProductOptions } from '@/features/products/use-product-options';
import {
  cancelSale,
  DEDUCTION_TYPE_LABEL,
  getSale,
  SALE_STATUS_LABEL,
  SETTLEMENT_STATUS_LABEL,
  UNIT_LABEL,
} from '@/features/sales/sales-api';
import { isApiError } from '@/lib/api-client';
import type { SaleDetail } from '@/shared/entities';

const TRY = new Intl.NumberFormat('tr-TR', {
  style: 'currency',
  currency: 'TRY',
});
const PCT = new Intl.NumberFormat('tr-TR', { style: 'percent', maximumFractionDigits: 2 });

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? '—' : d.toLocaleDateString('tr-TR');
}

export default function SaleDetailPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const id = params.id;

  const { options: products } = useProductOptions();
  const [sale, setSale] = useState<SaleDetail | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionError, setActionError] = useState<string | null>(null);
  const [cancelling, setCancelling] = useState(false);

  const productName = useMemo(() => {
    const map = new Map(products.map((p) => [p.id, p.name]));
    return (pid: string) => map.get(pid) ?? pid.slice(0, 8);
  }, [products]);

  function reload() {
    setLoading(true);
    getSale(id)
      .then((s) => setSale(s))
      .catch((err) =>
        setLoadError(isApiError(err) ? err.message : 'Satış yüklenemedi.'),
      )
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    let cancelled = false;
    getSale(id)
      .then((s) => {
        if (!cancelled) setSale(s);
      })
      .catch((err) => {
        if (!cancelled)
          setLoadError(isApiError(err) ? err.message : 'Satış yüklenemedi.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  async function handleCancel() {
    const reason = window.prompt('İptal gerekçesi (denetim izi için zorunlu):');
    if (reason === null) return;
    if (!reason.trim()) {
      setActionError('İptal gerekçesi zorunludur.');
      return;
    }
    setActionError(null);
    setCancelling(true);
    try {
      await cancelSale(id, reason.trim());
      reload();
    } catch (err) {
      setActionError(isApiError(err) ? err.message : 'İptal edilemedi.');
    } finally {
      setCancelling(false);
    }
  }

  if (loading) return <p className="page-state">Yükleniyor…</p>;
  if (loadError)
    return <p className="page-state page-state--error">{loadError}</p>;
  if (!sale) return <p className="page-state">Satış bulunamadı.</p>;

  const canCancel = sale.status === 2 && !sale.isCancelled; // yalnız tamamlanmış ve iptal edilmemiş

  return (
    <div className="form-page form-page--wide">
      <div className="page-head">
        <h1 className="page-title">Satış Detayı</h1>
        <span className={sale.status === 3 ? 'badge badge--error' : 'badge badge--ok'}>
          {label(SALE_STATUS_LABEL, sale.status)}
        </span>
      </div>

      {actionError ? <div className="form-error">{actionError}</div> : null}

      <div className="detail-meta">
        <span><strong>Tarih:</strong> {formatDate(sale.soldAt)}</span>
        <span><strong>Alıcı:</strong> {sale.buyerPartyId.slice(0, 8)}</span>
        <span><strong>Müstahsil:</strong> {sale.producerPartyId.slice(0, 8)}</span>
        <span><strong>Hal içi:</strong> {sale.isWithinMarket ? 'Evet' : 'Hayır'}</span>
        <span><strong>Brüt:</strong> {TRY.format(sale.grossAmount)}</span>
      </div>

      {sale.isCancelled && sale.cancellationReason ? (
        <div className="form-warn">İptal gerekçesi: {sale.cancellationReason}</div>
      ) : null}

      <h2 className="section-title">Satırlar</h2>
      <table className="data-table">
        <thead>
          <tr>
            <th>Ürün</th>
            <th className="data-table__num">Miktar</th>
            <th>Birim</th>
            <th className="data-table__num">Birim Fiyat</th>
            <th className="data-table__num">Tutar</th>
          </tr>
        </thead>
        <tbody>
          {sale.lines.map((l) => (
            <tr key={l.id}>
              <td>{productName(l.productId)}</td>
              <td className="data-table__num">{l.quantity}</td>
              <td>{label(UNIT_LABEL, l.unit)}</td>
              <td className="data-table__num">{TRY.format(l.unitPrice)}</td>
              <td className="data-table__num">{TRY.format(l.lineAmount)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {sale.commissionCalculation ? (
        <>
          <h2 className="section-title">Komisyon</h2>
          <div className="detail-meta">
            <span>
              <strong>Oran:</strong> {PCT.format(sale.commissionCalculation.commissionRate)}
            </span>
            <span>
              <strong>Komisyon:</strong>{' '}
              {TRY.format(sale.commissionCalculation.commissionAmount)}
            </span>
            <span>
              <strong>KDV ({PCT.format(sale.commissionCalculation.vatRate)}):</strong>{' '}
              {TRY.format(sale.commissionCalculation.vatAmount)}
            </span>
          </div>
        </>
      ) : null}

      {sale.deductions.length > 0 ? (
        <>
          <h2 className="section-title">Kesintiler</h2>
          <table className="data-table">
            <thead>
              <tr>
                <th>Tür</th>
                <th className="data-table__num">Oran</th>
                <th className="data-table__num">Tutar</th>
              </tr>
            </thead>
            <tbody>
              {sale.deductions.map((d, i) => (
                <tr key={`${d.type}-${i}`}>
                  <td>{label(DEDUCTION_TYPE_LABEL, d.type)}</td>
                  <td className="data-table__num">{PCT.format(d.rate)}</td>
                  <td className="data-table__num">{TRY.format(d.amount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      ) : null}

      {sale.settlement ? (
        <>
          <h2 className="section-title">Hakediş (Müstahsil)</h2>
          <div className="detail-meta">
            <span>
              <strong>Net:</strong> {TRY.format(sale.settlement.netAmount)}
            </span>
            <span>
              <strong>Vade:</strong> {formatDate(sale.settlement.dueDate)}
            </span>
            <span>
              <strong>Durum:</strong>{' '}
              {label(SETTLEMENT_STATUS_LABEL, sale.settlement.status)}
            </span>
          </div>
        </>
      ) : null}

      <div className="form-actions form-actions--split">
        <button
          type="button"
          className="btn-danger"
          onClick={handleCancel}
          disabled={!canCancel || cancelling}
        >
          {cancelling ? 'İptal ediliyor…' : 'Satışı İptal Et'}
        </button>
        <button
          type="button"
          className="btn-secondary"
          onClick={() => router.push('/dashboard/satis')}
        >
          Geri
        </button>
      </div>
    </div>
  );
}

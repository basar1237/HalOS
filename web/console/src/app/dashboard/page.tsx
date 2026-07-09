'use client';

// Kontrol paneli — okuma-modeli metrikleri (API Gateway üzerinden) + canlı SignalR akışı.
// "Günlük Satış" backend günlük özetiyle dolar ve yeni satış bildirimleriyle canlı artar.
// Cari yaşlandırma ve düşük stok kartları rapor uçlarından gelir. Kalan kartlar (bekleyen
// hakediş / e-belge / mal geliş) uygun özet uç eklenince bağlanacak (şimdilik "Veri yok").

import { useMemo } from 'react';

import { useDashboardMetrics } from '@/features/dashboard/use-dashboard-metrics';
import {
  NotificationType,
  readNumber,
  type ConnectionStatus,
} from '@/features/realtime/types';
import { useDashboardFeed } from '@/features/realtime/use-dashboard-feed';

const STATUS_LABEL: Record<ConnectionStatus, string> = {
  disconnected: 'Bağlantı yok',
  connecting: 'Bağlanıyor…',
  connected: 'Canlı',
  reconnecting: 'Yeniden bağlanıyor…',
};

const TRY = new Intl.NumberFormat('tr-TR', {
  style: 'currency',
  currency: 'TRY',
});

function formatTime(iso: string): string {
  const date = new Date(iso);
  return Number.isNaN(date.getTime())
    ? '—'
    : date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
}

export default function DashboardPage() {
  const { notifications, status, clear } = useDashboardFeed();
  const { daily, aging, lowStock, salesDashboard, pendingDocuments } =
    useDashboardMetrics();

  // Bu oturumda gelen canlı satışların net toplamı ve adedi (backend günlük özetine eklenir).
  const liveSales = useMemo(() => {
    const sales = notifications.filter(
      (n) => n.type === NotificationType.SaleCompleted,
    );
    const net = sales.reduce(
      (sum, n) => sum + (readNumber(n.payload, 'netAmount') ?? 0),
      0,
    );
    return { count: sales.length, net };
  }, [notifications]);

  // Görüntülenen günlük net = backend günlük net + oturum içi canlı satışlar.
  const dailyNet = (daily.data?.net ?? 0) + liveSales.net;
  const dailyCount = (daily.data?.count ?? 0) + liveSales.count;

  return (
    <div>
      <div className="page-head">
        <h1 className="page-title">Kontrol Paneli</h1>
        <span className={`status-pill status-pill--${status}`}>
          <span className="status-pill__dot" />
          {STATUS_LABEL[status]}
        </span>
      </div>

      <div className="card-grid">
        {/* Günlük Satış — backend özeti + canlı artış */}
        <section className="card card--live">
          <p className="card__title">Günlük Satış (canlı)</p>
          {daily.loading ? (
            <p className="card__placeholder">Yükleniyor…</p>
          ) : daily.error ? (
            <p className="card__error">{daily.error}</p>
          ) : (
            <>
              <p className="card__metric">{TRY.format(dailyNet)}</p>
              <p className="card__sub">{dailyCount} satış · bugün</p>
            </>
          )}
        </section>

        {/* Açık Cari Bakiye — cari yaşlandırma toplamı */}
        <section className="card">
          <p className="card__title">Açık Cari Bakiye</p>
          {aging.loading ? (
            <p className="card__placeholder">Yükleniyor…</p>
          ) : aging.error ? (
            <p className="card__error">{aging.error}</p>
          ) : aging.data ? (
            <>
              <p className="card__metric">{TRY.format(aging.data.totalAmount)}</p>
              <p className="card__sub">
                {aging.data.totalAccountCount} cari ·{' '}
                {TRY.format(aging.data.days31Plus.amount)} 31+ gün
              </p>
            </>
          ) : (
            <p className="card__placeholder">Veri yok</p>
          )}
        </section>

        {/* Düşük Stok Uyarısı — yeniden-sipariş eşiği altındaki kalemler */}
        <section className="card">
          <p className="card__title">Düşük Stok Uyarısı</p>
          {lowStock.loading ? (
            <p className="card__placeholder">Yükleniyor…</p>
          ) : lowStock.error ? (
            <p className="card__error">{lowStock.error}</p>
          ) : lowStock.data ? (
            <>
              <p className="card__metric">{lowStock.data.length}</p>
              <p className="card__sub">eşik altı ürün</p>
            </>
          ) : (
            <p className="card__placeholder">Veri yok</p>
          )}
        </section>

        {/* Bekleyen Hakediş — ödenmemiş müstahsil hakedişi toplamı */}
        <section className="card">
          <p className="card__title">Bekleyen Hakediş</p>
          {salesDashboard.loading ? (
            <p className="card__placeholder">Yükleniyor…</p>
          ) : salesDashboard.error ? (
            <p className="card__error">{salesDashboard.error}</p>
          ) : salesDashboard.data ? (
            <p className="card__metric">
              {TRY.format(salesDashboard.data.pendingSettlementTotal)}
            </p>
          ) : (
            <p className="card__placeholder">Veri yok</p>
          )}
        </section>

        {/* Bekleyen e-Belge — Draft/Failed e-Fatura+e-MM+HKS toplamı */}
        <section className="card">
          <p className="card__title">Bekleyen e-Belge</p>
          {pendingDocuments.loading ? (
            <p className="card__placeholder">Yükleniyor…</p>
          ) : pendingDocuments.error ? (
            <p className="card__error">{pendingDocuments.error}</p>
          ) : pendingDocuments.data ? (
            <>
              <p className="card__metric">{pendingDocuments.data.total}</p>
              <p className="card__sub">
                {pendingDocuments.data.pendingInvoices} e-Fatura ·{' '}
                {pendingDocuments.data.pendingProducerReceipts} e-MM ·{' '}
                {pendingDocuments.data.pendingHksNotifications} HKS
              </p>
            </>
          ) : (
            <p className="card__placeholder">Veri yok</p>
          )}
        </section>

        {/* Bugünkü Mal Geliş — bugün kabul edilen konsinye partisi adedi */}
        <section className="card">
          <p className="card__title">Bugünkü Mal Geliş</p>
          {salesDashboard.loading ? (
            <p className="card__placeholder">Yükleniyor…</p>
          ) : salesDashboard.error ? (
            <p className="card__error">{salesDashboard.error}</p>
          ) : salesDashboard.data ? (
            <>
              <p className="card__metric">
                {salesDashboard.data.todayConsignmentCount}
              </p>
              <p className="card__sub">parti · bugün</p>
            </>
          ) : (
            <p className="card__placeholder">Veri yok</p>
          )}
        </section>
      </div>

      <section className="feed">
        <div className="feed__head">
          <h2 className="feed__title">Canlı Akış</h2>
          {notifications.length > 0 && (
            <button type="button" className="feed__clear" onClick={clear}>
              Temizle
            </button>
          )}
        </div>
        {notifications.length === 0 ? (
          <p className="feed__empty">
            Henüz bildirim yok. Yeni satışlar burada anlık görünür.
          </p>
        ) : (
          <ul className="feed__list">
            {notifications.map((n, index) => (
              <li key={`${n.occurredOnUtc}-${index}`} className="feed__item">
                <div className="feed__item-main">
                  <span className="feed__item-title">{n.title}</span>
                  <span className="feed__item-message">{n.message}</span>
                </div>
                <time className="feed__item-time">
                  {formatTime(n.occurredOnUtc)}
                </time>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

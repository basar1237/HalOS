'use client';

// Kontrol paneli — canlı SignalR dashboard (docs/06 S2.2). Notification servisinden gelen
// bildirimlerle "Günlük Satış" kartı ve canlı akış gerçek zamanlı güncellenir. Diğer kartlar
// ileriki fazda kendi okuma-modeli uçlarına bağlanacak (şimdilik iskelet).

import { useMemo } from 'react';

import { useDashboardFeed } from '@/features/realtime/use-dashboard-feed';
import {
  NotificationType,
  readNumber,
  type ConnectionStatus,
} from '@/features/realtime/types';

const STATUS_LABEL: Record<ConnectionStatus, string> = {
  disconnected: 'Bağlantı yok',
  connecting: 'Bağlanıyor…',
  connected: 'Canlı',
  reconnecting: 'Yeniden bağlanıyor…',
};

// Tutar biçimi — TR yerelleştirme (docs/02 sözlük: TL). Backend kültür-bağımsız taşır.
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

  // Bu oturumda alınan satış bildirimlerinden canlı net toplam. Kalıcı gün-toplamı değil;
  // sayfa açıkken biriken canlı özet (gerçek gün-toplamı Sales okuma-modelinden gelecek).
  const liveSalesTotal = useMemo(
    () =>
      notifications
        .filter((n) => n.type === NotificationType.SaleCompleted)
        .reduce((sum, n) => sum + (readNumber(n.payload, 'netAmount') ?? 0), 0),
    [notifications],
  );

  const saleCount = notifications.filter(
    (n) => n.type === NotificationType.SaleCompleted,
  ).length;

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
        <section className="card card--live">
          <p className="card__title">Günlük Satış (canlı)</p>
          <p className="card__metric">{TRY.format(liveSalesTotal)}</p>
          <p className="card__sub">{saleCount} satış · oturum içi</p>
        </section>
        <section className="card">
          <p className="card__title">Bekleyen Hakediş</p>
          <p className="card__placeholder">Veri yok</p>
        </section>
        <section className="card">
          <p className="card__title">Açık Cari Bakiye</p>
          <p className="card__placeholder">Veri yok</p>
        </section>
        <section className="card">
          <p className="card__title">Bugünkü Mal Geliş</p>
          <p className="card__placeholder">Veri yok</p>
        </section>
        <section className="card">
          <p className="card__title">Bekleyen e-Belge</p>
          <p className="card__placeholder">Veri yok</p>
        </section>
        <section className="card">
          <p className="card__title">Soğuk Zincir Uyarıları</p>
          <p className="card__placeholder">Veri yok</p>
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

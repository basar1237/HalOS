'use client';

// Bildirimler — canlı SignalR akışının tam sayfa görünümü (docs/06 S2.2). Dashboard'daki
// akışla aynı kaynağı (useDashboardFeed) kullanır; bu sayfa geçmişi daha geniş listeler.

import { useDashboardFeed } from '@/features/realtime/use-dashboard-feed';
import type { ConnectionStatus } from '@/features/realtime/types';

const STATUS_LABEL: Record<ConnectionStatus, string> = {
  disconnected: 'Bağlantı yok',
  connecting: 'Bağlanıyor…',
  connected: 'Canlı',
  reconnecting: 'Yeniden bağlanıyor…',
};

function formatTime(iso: string): string {
  const date = new Date(iso);
  return Number.isNaN(date.getTime())
    ? '—'
    : date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
}

export default function NotificationsPage() {
  const { notifications, status, clear } = useDashboardFeed();

  return (
    <div>
      <div className="page-head">
        <h1 className="page-title">Bildirimler</h1>
        <span className={`status-pill status-pill--${status}`}>
          <span className="status-pill__dot" />
          {STATUS_LABEL[status]}
        </span>
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

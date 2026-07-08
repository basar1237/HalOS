'use client';

// Canlı dashboard SignalR bağlantı hook'u (docs/06 S2.2).
// Notification servisinin /hubs/dashboard hub'ına bağlanır, "notify" metodundan gelen
// bildirimleri toplar ve bağlantı durumunu izler. Tenant izolasyonu SUNUCUDA yapılır
// (hub, JWT tenant_id claim'inden grubu belirler — istemci grup seçemez, BK-8); bu yüzden
// istemci yalnızca token'ı sağlar, tenant'ı ASLA parametre olarak GÖNDERMEZ.

import { useCallback, useEffect, useRef, useState } from 'react';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

import { getAccessToken } from '@/lib/token-storage';
import type { ConnectionStatus, DashboardNotification } from './types';

/** Hub istemci metodu — backend DashboardHub.ClientNotifyMethod ile aynı olmalı. */
const NOTIFY_METHOD = 'notify';

/** Bellekte tutulan bildirim tavanı; eski kayıtlar düşer (kalıcılık yok, salt canlı akış). */
const MAX_FEED = 50;

const NOTIFICATION_URL =
  process.env.NEXT_PUBLIC_NOTIFICATION_URL ?? 'http://localhost:5096';

export interface DashboardFeed {
  /** En yeni önce sıralı canlı bildirimler (en fazla MAX_FEED). */
  notifications: DashboardNotification[];
  status: ConnectionStatus;
  /** Akışı temizler (yalnız istemci görünümü; sunucuyu etkilemez). */
  clear: () => void;
}

/**
 * Canlı dashboard akışına abone olur. Yalnızca oturum açık (token var) iken bağlanır;
 * bileşen sökülünce bağlantı düzgün kapatılır. Otomatik yeniden bağlanma açıktır.
 */
export function useDashboardFeed(): DashboardFeed {
  const [notifications, setNotifications] = useState<DashboardNotification[]>(
    [],
  );
  const [status, setStatus] = useState<ConnectionStatus>('disconnected');
  const connectionRef = useRef<HubConnection | null>(null);

  const clear = useCallback(() => setNotifications([]), []);

  useEffect(() => {
    // Token yoksa bağlanma; hub [Authorize] olduğundan reddedilir. Auth akışı yönlendirir.
    const token = getAccessToken();
    if (!token) {
      setStatus('disconnected');
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${NOTIFICATION_URL}/hubs/dashboard`, {
        // WebSocket el sıkışmasında Authorization başlığı gönderilemez → token query
        // string'de access_token olarak taşınır (backend JwtBearerEvents ile eşleşir).
        // Fabrika her (yeniden) bağlanmada çağrılır; yenilenen token'ı kullanır.
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;
    let disposed = false;

    connection.on(NOTIFY_METHOD, (notification: DashboardNotification) => {
      setNotifications((prev) => [notification, ...prev].slice(0, MAX_FEED));
    });

    connection.onreconnecting(() => setStatus('reconnecting'));
    connection.onreconnected(() => setStatus('connected'));
    connection.onclose(() => {
      if (!disposed) setStatus('disconnected');
    });

    setStatus('connecting');
    connection
      .start()
      .then(() => {
        if (!disposed) setStatus('connected');
      })
      .catch(() => {
        if (!disposed) setStatus('disconnected');
      });

    return () => {
      disposed = true;
      connectionRef.current = null;
      // stop() yalnız kurulmuş/kurulan bağlantıda çağrılmalı; aksi halde sessizce yut.
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop();
      }
    };
  }, []);

  return { notifications, status, clear };
}

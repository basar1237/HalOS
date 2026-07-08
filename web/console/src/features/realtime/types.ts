// Canlı dashboard bildirim tipleri (docs/06 S2.2). Backend sözleşmesiyle birebir:
// HalOS.Notification.Domain.DashboardNotification (SignalR JSON, camelCase alanlar).

/** Kanonik bildirim tür kodları — backend NotificationTypes sabitleriyle aynı. */
export const NotificationType = {
  /** Bir satış tamamlandığında (kaynak: SaleCompleted). */
  SaleCompleted: 'sale.completed',
} as const;

export type NotificationTypeCode =
  (typeof NotificationType)[keyof typeof NotificationType];

/**
 * Hub'ın "notify" metoduyla gelen tek bildirim. Backend DashboardNotification
 * record'unun JSON karşılığı (System.Text.Json camelCase). Payload serbest biçimlidir;
 * sale.completed için saleTransactionId/grossAmount/netAmount/soldAt taşır.
 */
export interface DashboardNotification {
  type: string;
  tenantId: string;
  title: string;
  message: string;
  payload: Record<string, unknown> | null;
  occurredOnUtc: string;
}

/** SignalR bağlantı durumu — UI göstergesi için. */
export type ConnectionStatus =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting';

/** sale.completed payload'ından güvenli sayı okuma yardımcıları. */
export function readNumber(
  payload: Record<string, unknown> | null,
  key: string,
): number | null {
  const value = payload?.[key];
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

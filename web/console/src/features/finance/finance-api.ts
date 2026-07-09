// Finance yazma çağrıları — müstahsile ödeme / alıcıdan tahsilat / avans (docs/03 M6).
// Üçü aynı istek şeklini paylaşır: POST /api/finance/{payments|collections|advances}.
// BK-6: 7.000 TL üstü nakit backend'de reddedilir (kanal Bank olmalı).

import { apiClient } from '@/lib/api-client';

/** PaymentChannel enum (backend): Cash=1, Bank=2. */
export const PaymentChannel = {
  Cash: 1,
  Bank: 2,
} as const;

export type MovementKind = 'payment' | 'collection' | 'advance';

export interface RecordMovementRequest {
  partyId: string;
  amount: number;
  channel: number;
  bankReference: string | null;
  occurredAt: string;
}

const PATH: Record<MovementKind, string> = {
  payment: '/api/finance/payments',
  collection: '/api/finance/collections',
  advance: '/api/finance/advances',
};

export const MOVEMENT_TITLE: Record<MovementKind, string> = {
  payment: 'Müstahsile Ödeme',
  collection: 'Alıcıdan Tahsilat',
  advance: 'Avans',
};

export function isMovementKind(value: string | null): value is MovementKind {
  return value === 'payment' || value === 'collection' || value === 'advance';
}

export function recordMovement(
  kind: MovementKind,
  request: RecordMovementRequest,
): Promise<unknown> {
  return apiClient.post(PATH[kind], request);
}

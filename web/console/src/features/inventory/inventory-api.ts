// Inventory yazma çağrıları. Fire (zayiat) kaydı — docs/03 M9 / BK-7.
// POST /api/inventory/spoilage; BK-7: fire mevcut stoğu aşamaz (kalan negatif olamaz) → backend zorlar.

import { apiClient } from '@/lib/api-client';

export interface RecordSpoilageRequest {
  productId: string;
  quantity: number;
  reason: string;
  occurredAt: string;
}

export function recordSpoilage(request: RecordSpoilageRequest): Promise<unknown> {
  return apiClient.post('/api/inventory/spoilage', request);
}

// Sales yazma/okuma çağrıları + enum etiketleri (docs/03 M4). Satış çok-adımlı aggregate:
// oluştur (taslak) → satır ekle → tamamla (kesinti/hakediş motoru, SaleCompleted yayılır BK-1).
// createCompleteSale bu üç adımı tek akışta orkestralar.

import { apiClient } from '@/lib/api-client';

/** SaleTerm (backend): Cash=1 (peşin, 15 iş günü), Deferred=2 (vadeli, 30 gün) — BK-3. */
export const SaleTerm = { Cash: 1, Deferred: 2 } as const;

export const SALE_TERM_LABEL: Record<number, string> = {
  1: 'Peşin',
  2: 'Vadeli',
};

/** UnitOfMeasure (backend): 1-5. */
export const UnitOfMeasure = {
  Crate: 1,
  Kilogram: 2,
  Sack: 3,
  Piece: 4,
  Box: 5,
} as const;

export const UNIT_LABEL: Record<number, string> = {
  1: 'Kasa',
  2: 'Kg',
  3: 'Çuval',
  4: 'Adet',
  5: 'Sandık',
};

/** SaleStatus (backend): Draft=1, Completed=2, Cancelled=3. */
export const SALE_STATUS_LABEL: Record<number, string> = {
  1: 'Taslak',
  2: 'Tamamlandı',
  3: 'İptal',
};

export interface CreateSaleHeader {
  buyerPartyId: string;
  producerPartyId: string;
  soldAt: string;
  isWithinMarket: boolean;
  term: number;
}

export interface SaleLineInput {
  productId: string;
  quantity: number;
  unit: number;
  unitPrice: number;
}

function createSale(
  header: CreateSaleHeader,
  operationId: string,
): Promise<{ id: string }> {
  return apiClient.post<{ id: string }>('/api/sales/sales', {
    buyerPartyId: header.buyerPartyId,
    producerPartyId: header.producerPartyId,
    consignmentId: null,
    soldAt: header.soldAt,
    isWithinMarket: header.isWithinMarket,
    operationId,
    term: header.term,
  });
}

function addSaleLine(saleId: string, line: SaleLineInput): Promise<unknown> {
  return apiClient.post(`/api/sales/sales/${saleId}/lines`, line);
}

function completeSale(saleId: string): Promise<unknown> {
  return apiClient.post(`/api/sales/sales/${saleId}/complete`);
}

/**
 * Satışı uçtan uca kaydeder: oluştur → satırları ekle → tamamla. OperationId istemcide üretilir
 * (offline idempotency, docs/04 §5). Herhangi bir adım hata verirse fırlatır (taslak backend'de
 * kalabilir; kullanıcı yeniden deneyebilir). Tamamlanan satışın kimliğini döndürür.
 */
export async function createCompleteSale(
  header: CreateSaleHeader,
  lines: SaleLineInput[],
): Promise<{ id: string }> {
  const operationId = crypto.randomUUID();
  const { id } = await createSale(header, operationId);
  for (const line of lines) {
    await addSaleLine(id, line);
  }
  await completeSale(id);
  return { id };
}

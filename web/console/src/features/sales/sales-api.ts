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

/** DeductionType (backend): 1-5. */
export const DEDUCTION_TYPE_LABEL: Record<number, string> = {
  1: 'Komisyon',
  2: 'Zirai Stopaj',
  3: 'Çiftçi Bağ-Kur',
  4: 'Hal Rüsumu',
  5: 'KDV',
};

/** SettlementStatus (backend): Pending=1, Scheduled=2, Paid=3. */
export const SETTLEMENT_STATUS_LABEL: Record<number, string> = {
  1: 'Beklemede',
  2: 'Planlandı',
  3: 'Ödendi',
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
/** Tekil satış (lines/komisyon/kesinti/hakediş dahil) — GET /api/sales/sales/{id}. */
export function getSale(id: string): Promise<import('@/shared/entities').SaleDetail> {
  return apiClient.get(`/api/sales/sales/${id}`);
}

/** Satışı iptal eder (ters kayıt/flag; SİLİNMEZ — BK-9). POST /api/sales/sales/{id}/cancel. */
export function cancelSale(id: string, reason: string): Promise<unknown> {
  return apiClient.post(`/api/sales/sales/${id}/cancel`, { reason });
}

export interface ConsignmentItemInput {
  productId: string;
  quantity: number;
  unit: number;
}

export interface ReceiveConsignmentRequest {
  producerPartyId: string;
  receivedAt: string;
  dispatchNoteRef: string | null;
  items: ConsignmentItemInput[];
}

/** Müstahsilden mal geliş partisi kabul eder (docs/03 M3). POST /api/sales/consignments. */
export function receiveConsignment(
  request: ReceiveConsignmentRequest,
): Promise<{ id: string }> {
  return apiClient.post<{ id: string }>('/api/sales/consignments', request);
}

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

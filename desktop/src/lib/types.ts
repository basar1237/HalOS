// Hal Terminali ortak tipleri. Backend enum'ları JSON'da INTEGER'dır (web/mobil ile aynı
// kural — bkz web/console labels). Burada da 1-tabanlı sayısal enum kullanılır.

export type SaleTerm = 1 | 2; // 1 = Peşin, 2 = Vadeli
export type UnitOfMeasure = 1 | 2 | 3 | 4 | 5; // kg / adet / kasa / bağ / demet
export type PartyType = 1 | 2 | 3 | 4; // Producer / Buyer / Merchant / Consignor

export type SyncStatus = 'pending' | 'synced' | 'conflict';
export type OutboxStatus = 'pending' | 'sending' | 'synced' | 'failed';

/** Buluta gönderilecek bir işlem — yerelde commit edilmiş, kuyrukta bekliyor (docs/04 §5). */
export interface OutboxEntry {
  /** İstemci üretimli UUID → idempotency anahtarı; çift senkron güvenli. */
  operationId: string;
  aggregateType: string; // 'sale' | 'consignment' | ...
  aggregateId: string;
  /** Aynı aggregate içinde monoton artan sıra → per-aggregate sıralı gönderim. */
  seq: number;
  opType: string; // 'create-sale' | 'cancel-sale' | ...
  payload: unknown;
  status: OutboxStatus;
  attempts: number;
  lastError?: string | null;
  createdAt: string;
  syncedAt?: string | null;
}

export interface SaleLineInput {
  productId: string;
  productName: string;
  quantity: number;
  unitCode: UnitOfMeasure;
  unitPrice: number;
}

export interface LocalSale {
  operationId: string;
  serverId?: string | null;
  partyId: string;
  partyName: string;
  /** Müstahsil (Producer) referansı — hakediş bu tarafa; hal satışında zorunlu (docs/02 §3.3). */
  producerPartyId: string;
  saleTerm: SaleTerm;
  isWithinMarket: boolean;
  lines: SaleLineInput[];
  grossTotal: number;
  status: 'draft' | 'completed' | 'cancelled';
  syncStatus: SyncStatus;
  createdAt: string;
}

export interface CachedProduct {
  id: string;
  name: string;
  defaultUnit: UnitOfMeasure;
  rowVersion?: string | null;
  updatedAt: string;
}

export interface CachedParty {
  id: string;
  name: string;
  partyType: PartyType;
  rowVersion?: string | null;
  updatedAt: string;
}

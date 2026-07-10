// Yerel SQLite erişim katmanı. Alt seviyede @tauri-apps/plugin-sql kullanılır; ancak
// tüm iş mantığı bu ince katmanın ÜSTÜNDEki saf modüllerdedir (outbox/sync/money/conflict).
// LocalDb arayüzü, testlerde gerçek Tauri olmadan sahte (fake) ile değiştirilebilir olması içindir.

import type {
  CachedParty,
  CachedProduct,
  LocalSale,
  OutboxEntry,
} from './types';

export interface QueryResult {
  rowsAffected: number;
  lastInsertId?: number;
}

/** plugin-sql'in Database örneğiyle uyumlu minimal arayüz. */
export interface LocalDb {
  execute(query: string, values?: unknown[]): Promise<QueryResult>;
  select<T>(query: string, values?: unknown[]): Promise<T[]>;
}

let dbPromise: Promise<LocalDb> | null = null;

/** Tauri çalışma zamanında gerçek SQLite'ı yükler (yalnız uygulama içinde çağrılır, testte değil). */
export function getDb(): Promise<LocalDb> {
  if (!dbPromise) {
    dbPromise = import('@tauri-apps/plugin-sql').then((m) =>
      m.default.load('sqlite:halos-terminal.db'),
    ) as Promise<LocalDb>;
  }
  return dbPromise;
}

// ---- Sync motorunun ihtiyaç duyduğu depo arayüzleri (sync.ts saf tutulsun diye) ----

export interface SyncStore {
  loadOutbox(): Promise<OutboxEntry[]>;
  updateOutboxEntry(entry: OutboxEntry): Promise<void>;
  markSaleSynced(operationId: string, serverId: string | undefined): Promise<void>;
  upsertProducts(items: CachedProduct[]): Promise<void>;
  upsertParties(items: CachedParty[]): Promise<void>;
}

// ---- Somut LocalDb tabanlı depo (uygulama içi) ----

interface OutboxRow {
  operation_id: string;
  aggregate_type: string;
  aggregate_id: string;
  seq: number;
  op_type: string;
  payload: string;
  status: OutboxEntry['status'];
  attempts: number;
  last_error: string | null;
  created_at: string;
  synced_at: string | null;
}

function rowToOutbox(r: OutboxRow): OutboxEntry {
  return {
    operationId: r.operation_id,
    aggregateType: r.aggregate_type,
    aggregateId: r.aggregate_id,
    seq: r.seq,
    opType: r.op_type,
    payload: JSON.parse(r.payload) as unknown,
    status: r.status,
    attempts: r.attempts,
    lastError: r.last_error,
    createdAt: r.created_at,
    syncedAt: r.synced_at,
  };
}

export function createSyncStore(db: LocalDb): SyncStore {
  return {
    async loadOutbox() {
      const rows = await db.select<OutboxRow>('SELECT * FROM outbox ORDER BY id ASC');
      return rows.map(rowToOutbox);
    },

    async updateOutboxEntry(entry) {
      await db.execute(
        `INSERT INTO outbox
           (operation_id, aggregate_type, aggregate_id, seq, op_type, payload, status, attempts, last_error, created_at, synced_at)
         VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11)
         ON CONFLICT(operation_id) DO UPDATE SET
           status = excluded.status,
           attempts = excluded.attempts,
           last_error = excluded.last_error,
           synced_at = excluded.synced_at`,
        [
          entry.operationId,
          entry.aggregateType,
          entry.aggregateId,
          entry.seq,
          entry.opType,
          JSON.stringify(entry.payload),
          entry.status,
          entry.attempts,
          entry.lastError ?? null,
          entry.createdAt,
          entry.syncedAt ?? null,
        ],
      );
    },

    async markSaleSynced(operationId, serverId) {
      await db.execute(
        `UPDATE local_sale SET sync_status = 'synced', server_id = $2 WHERE operation_id = $1`,
        [operationId, serverId ?? null],
      );
    },

    async upsertProducts(items) {
      for (const p of items) {
        await db.execute(
          `INSERT INTO cached_product (id, name, default_unit, row_version, updated_at)
           VALUES ($1,$2,$3,$4,$5)
           ON CONFLICT(id) DO UPDATE SET name=excluded.name, default_unit=excluded.default_unit,
             row_version=excluded.row_version, updated_at=excluded.updated_at`,
          [p.id, p.name, p.defaultUnit, p.rowVersion ?? null, p.updatedAt],
        );
      }
    },

    async upsertParties(items) {
      for (const p of items) {
        await db.execute(
          `INSERT INTO cached_party (id, name, party_type, row_version, updated_at)
           VALUES ($1,$2,$3,$4,$5)
           ON CONFLICT(id) DO UPDATE SET name=excluded.name, party_type=excluded.party_type,
             row_version=excluded.row_version, updated_at=excluded.updated_at`,
          [p.id, p.name, p.partyType, p.rowVersion ?? null, p.updatedAt],
        );
      }
    },
  };
}

// ---- Yerel satış yazımı (offline commit) ----

/**
 * Bir satışı yerelde commit eder VE outbox'a bir 'create-sale' işlemi ekler — tek mantıksal
 * birim (docs/04 §5: yerelde commit → kuyruğa al). operationId hem satışın hem işlemin kimliği.
 */
export async function commitSaleOffline(
  db: LocalDb,
  sale: LocalSale,
  seq: number,
): Promise<void> {
  await db.execute(
    `INSERT INTO local_sale
       (operation_id, party_id, party_name, sale_term, is_within_market, gross_total, status, sync_status, created_at)
     VALUES ($1,$2,$3,$4,$5,$6,$7,'pending',$8)`,
    [
      sale.operationId,
      sale.partyId,
      sale.partyName,
      sale.saleTerm,
      sale.isWithinMarket ? 1 : 0,
      String(sale.grossTotal),
      sale.status,
      sale.createdAt,
    ],
  );
  for (const line of sale.lines) {
    await db.execute(
      `INSERT INTO local_sale_line
         (sale_op_id, product_id, product_name, quantity, unit_code, unit_price, line_total)
       VALUES ($1,$2,$3,$4,$5,$6,$7)`,
      [
        sale.operationId,
        line.productId,
        line.productName,
        String(line.quantity),
        line.unitCode,
        String(line.unitPrice),
        String(line.quantity * line.unitPrice),
      ],
    );
  }
  await db.execute(
    `INSERT INTO outbox
       (operation_id, aggregate_type, aggregate_id, seq, op_type, payload, status, attempts, created_at)
     VALUES ($1,'sale',$2,$3,'create-sale',$4,'pending',0,$5)`,
    [
      sale.operationId,
      sale.operationId,
      seq,
      // Payload backend sözleşmesine göre (SyncOfflineSaleRequest); ASP.NET JSON eşlemesi
      // büyük/küçük harf duyarsızdır → camelCase alanlar PascalCase kayda eşlenir.
      JSON.stringify({
        buyerPartyId: sale.partyId,
        producerPartyId: sale.producerPartyId,
        soldAt: sale.createdAt,
        isWithinMarket: sale.isWithinMarket,
        term: sale.saleTerm,
        lines: sale.lines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unit: l.unitCode,
          unitPrice: l.unitPrice,
        })),
      }),
      sale.createdAt,
    ],
  );
}

interface SaleRow {
  operation_id: string;
  server_id: string | null;
  party_name: string;
  gross_total: string;
  sale_term: number;
  status: string;
  sync_status: string;
  created_at: string;
}

export interface SaleListItem {
  operationId: string;
  serverId: string | null;
  partyName: string;
  grossTotal: number;
  saleTerm: number;
  status: string;
  syncStatus: string;
  createdAt: string;
}

export async function listLocalSales(db: LocalDb): Promise<SaleListItem[]> {
  const rows = await db.select<SaleRow>(
    'SELECT * FROM local_sale ORDER BY created_at DESC LIMIT 200',
  );
  return rows.map((r) => ({
    operationId: r.operation_id,
    serverId: r.server_id,
    partyName: r.party_name,
    grossTotal: Number(r.gross_total),
    saleTerm: r.sale_term,
    status: r.status,
    syncStatus: r.sync_status,
    createdAt: r.created_at,
  }));
}

export async function listCachedProducts(db: LocalDb): Promise<CachedProduct[]> {
  const rows = await db.select<{
    id: string;
    name: string;
    default_unit: number;
    row_version: string | null;
    updated_at: string;
  }>('SELECT * FROM cached_product ORDER BY name ASC');
  return rows.map((r) => ({
    id: r.id,
    name: r.name,
    defaultUnit: r.default_unit as CachedProduct['defaultUnit'],
    rowVersion: r.row_version,
    updatedAt: r.updated_at,
  }));
}

export async function listCachedParties(db: LocalDb): Promise<CachedParty[]> {
  const rows = await db.select<{
    id: string;
    name: string;
    party_type: number;
    row_version: string | null;
    updated_at: string;
  }>('SELECT * FROM cached_party ORDER BY name ASC');
  return rows.map((r) => ({
    id: r.id,
    name: r.name,
    partyType: r.party_type as CachedParty['partyType'],
    rowVersion: r.row_version,
    updatedAt: r.updated_at,
  }));
}

/** Sonraki seq için mevcut outbox'tan aggregate'in en yüksek seq'ini bulur. */
export async function maxSeqForAggregate(db: LocalDb, aggregateId: string): Promise<number> {
  const rows = await db.select<{ m: number | null }>(
    'SELECT MAX(seq) AS m FROM outbox WHERE aggregate_id = $1',
    [aggregateId],
  );
  return rows[0]?.m ?? 0;
}

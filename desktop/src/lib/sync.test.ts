import { describe, expect, it } from 'vitest';
import type { SyncStore } from './db';
import { runSync, type SyncApi } from './sync';
import type { CachedParty, CachedProduct, OutboxEntry } from './types';

function makeStore(initial: OutboxEntry[]): SyncStore & {
  entries: Map<string, OutboxEntry>;
  syncedSales: { operationId: string; serverId?: string }[];
  products: CachedProduct[];
  parties: CachedParty[];
} {
  const entries = new Map<string, OutboxEntry>();
  for (const e of initial) entries.set(e.operationId, e);
  const syncedSales: { operationId: string; serverId?: string }[] = [];
  const products: CachedProduct[] = [];
  const parties: CachedParty[] = [];
  return {
    entries,
    syncedSales,
    products,
    parties,
    async loadOutbox() {
      return [...entries.values()];
    },
    async updateOutboxEntry(entry) {
      entries.set(entry.operationId, entry);
    },
    async markSaleSynced(operationId, serverId) {
      syncedSales.push({ operationId, serverId });
    },
    async upsertProducts(items) {
      products.push(...items);
    },
    async upsertParties(items) {
      parties.push(...items);
    },
  };
}

function entry(p: Partial<OutboxEntry>): OutboxEntry {
  return {
    operationId: p.operationId ?? 'op',
    aggregateType: 'sale',
    aggregateId: p.aggregateId ?? p.operationId ?? 'agg',
    seq: p.seq ?? 1,
    opType: p.opType ?? 'create-sale',
    payload: p.payload ?? {},
    status: p.status ?? 'pending',
    attempts: p.attempts ?? 0,
    lastError: null,
    createdAt: p.createdAt ?? '2026-07-10T10:00:00.000Z',
    syncedAt: null,
    ...p,
  };
}

const okApi: SyncApi = {
  async pushOperation(e) {
    return { serverId: `srv-${e.operationId}` };
  },
  async pullProducts() {
    return [{ id: 'p1', name: 'Domates', defaultUnit: 1, rowVersion: '1', updatedAt: '' }];
  },
  async pullParties() {
    return [{ id: 'q1', name: 'Ali Manav', partyType: 2, rowVersion: '1', updatedAt: '' }];
  },
};

const now = () => '2026-07-10T11:00:00.000Z';

describe('runSync', () => {
  it('bekleyen satışları gönderir ve senkron işaretler', async () => {
    const store = makeStore([
      entry({ operationId: 'a', aggregateId: 'a' }),
      entry({ operationId: 'b', aggregateId: 'b' }),
    ]);
    const summary = await runSync(store, okApi, { now });
    expect(summary.pushed).toBe(2);
    expect(summary.failed).toBe(0);
    expect([...store.entries.values()].every((e) => e.status === 'synced')).toBe(true);
    expect(store.syncedSales).toEqual([
      { operationId: 'a', serverId: 'srv-a' },
      { operationId: 'b', serverId: 'srv-b' },
    ]);
  });

  it('aynı aggregate\'in işlemlerini seq sırasıyla gönderir', async () => {
    const order: string[] = [];
    const api: SyncApi = {
      ...okApi,
      async pushOperation(e) {
        order.push(e.operationId);
        return { serverId: e.operationId };
      },
    };
    const store = makeStore([
      entry({ operationId: 'a2', aggregateId: 'a', seq: 2, opType: 'cancel-sale' }),
      entry({ operationId: 'a1', aggregateId: 'a', seq: 1 }),
    ]);
    await runSync(store, api, { now });
    expect(order).toEqual(['a1', 'a2']);
  });

  it('başarısız aggregate bloke olur ama diğeri devam eder; sonsuz döngü yok', async () => {
    const api: SyncApi = {
      ...okApi,
      async pushOperation(e) {
        if (e.aggregateId === 'a') throw { status: 500, message: 'sunucu hatası' };
        return { serverId: e.operationId };
      },
    };
    const store = makeStore([
      entry({ operationId: 'a1', aggregateId: 'a', seq: 1 }),
      entry({ operationId: 'a2', aggregateId: 'a', seq: 2 }),
      entry({ operationId: 'b1', aggregateId: 'b', seq: 1 }),
    ]);
    const summary = await runSync(store, api, { now });
    expect(summary.pushed).toBe(1); // yalnız b1
    expect(summary.failed).toBe(1); // a1 bir kez
    expect(store.entries.get('a1')?.status).toBe('failed');
    expect(store.entries.get('a1')?.attempts).toBe(1);
    expect(store.entries.get('a2')?.status).toBe('pending'); // sıra bozulmadı, gönderilmedi
    expect(store.entries.get('b1')?.status).toBe('synced');
    expect(summary.errors[0]).toContain('sunucu hatası');
  });

  it('master veriyi çeker (pull)', async () => {
    const store = makeStore([]);
    const summary = await runSync(store, okApi, { now });
    expect(summary.pulledProducts).toBe(1);
    expect(summary.pulledParties).toBe(1);
    expect(store.products[0].name).toBe('Domates');
    expect(store.parties[0].name).toBe('Ali Manav');
  });

  it('pull hatası push\'u bozmaz', async () => {
    const api: SyncApi = {
      async pushOperation(e) {
        return { serverId: e.operationId };
      },
      async pullProducts() {
        throw { status: 503, message: 'ürün çekilemedi' };
      },
      async pullParties() {
        return [];
      },
    };
    const store = makeStore([entry({ operationId: 'a', aggregateId: 'a' })]);
    const summary = await runSync(store, api, { now });
    expect(summary.pushed).toBe(1);
    expect(summary.errors.some((e) => e.includes('ürün çekilemedi'))).toBe(true);
  });
});

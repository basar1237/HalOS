// Sync motoru (docs/04 §5). Saf orkestrasyon: enjekte edilen depo (SyncStore) ve API üzerinden
// çalışır → gerçek Tauri/ağ olmadan Vitest ile test edilir.
//
// Push aşaması: outbox'tan HAZIR işlemleri (per-aggregate sıralı) buluta oynatır. Bir aggregate
// başarısız olursa yalnız o aggregate bu turda bloke olur; diğerleri devam eder. Aynı tur içinde
// başarısız işlem yeniden denenmez — yeniden deneme bir sonraki runSync çağrısında olur.
// Pull aşaması: master veriyi (ürün/taraf) buluttan çeker (sunucu otoriter → son-yazan-kazanır).

import type { SyncStore } from './db';
import { markFailed, markSending, markSynced, readyToSend } from './outbox';
import type { CachedParty, CachedProduct, OutboxEntry } from './types';

export interface SyncApi {
  pushOperation(entry: OutboxEntry): Promise<{ serverId?: string }>;
  pullProducts(): Promise<CachedProduct[]>;
  pullParties(): Promise<CachedParty[]>;
}

export interface SyncSummary {
  pushed: number;
  failed: number;
  pulledProducts: number;
  pulledParties: number;
  errors: string[];
}

function errorMessage(e: unknown): string {
  if (typeof e === 'object' && e !== null && 'message' in e) {
    return String((e as { message: unknown }).message);
  }
  return String(e);
}

export interface RunSyncOptions {
  now?: () => string;
  maxAttempts?: number;
}

/** Tarayıcı/WebView bağlantı durumu. offline iken sync atlanır. */
export function isOnline(): boolean {
  return typeof navigator === 'undefined' ? true : navigator.onLine;
}

export async function runSync(
  store: SyncStore,
  api: SyncApi,
  options: RunSyncOptions = {},
): Promise<SyncSummary> {
  const now = options.now ?? (() => new Date().toISOString());
  const summary: SyncSummary = { pushed: 0, failed: 0, pulledProducts: 0, pulledParties: 0, errors: [] };

  // --- PUSH ---
  const blocked = new Set<string>(); // bu turda başarısız olan aggregate'ler
  // Sonsuz döngü koruması: her tur en az bir aggregate ya ilerler ya bloke olur.
  for (let guard = 0; guard < 2000; guard++) {
    const entries = await store.loadOutbox();
    const ready = readyToSend(entries, options.maxAttempts).filter(
      (e) => !blocked.has(e.aggregateId),
    );
    if (ready.length === 0) break;

    for (const entry of ready) {
      try {
        await store.updateOutboxEntry(markSending(entry));
        const res = await api.pushOperation(entry);
        await store.updateOutboxEntry(markSynced(entry, now()));
        if (entry.opType === 'create-sale') {
          await store.markSaleSynced(entry.operationId, res.serverId);
        }
        summary.pushed++;
      } catch (e) {
        const msg = errorMessage(e);
        await store.updateOutboxEntry(markFailed(entry, msg));
        summary.failed++;
        summary.errors.push(`${entry.opType}/${entry.operationId}: ${msg}`);
        blocked.add(entry.aggregateId); // sıra bozulmasın → aggregate'i bu tur bloke et
      }
    }
  }

  // --- PULL (master veri) ---
  try {
    const products = await api.pullProducts();
    await store.upsertProducts(products);
    summary.pulledProducts = products.length;
  } catch (e) {
    summary.errors.push(`pull-products: ${errorMessage(e)}`);
  }
  try {
    const parties = await api.pullParties();
    await store.upsertParties(parties);
    summary.pulledParties = parties.length;
  } catch (e) {
    summary.errors.push(`pull-parties: ${errorMessage(e)}`);
  }

  return summary;
}

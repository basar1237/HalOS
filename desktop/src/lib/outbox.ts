// Outbox saf mantığı (docs/04 §5). SQL'e DOKUNMAZ → tümü Vitest ile test edilir.
// Kurallar:
//  - Her işlem idempotent (operationId). Çift gönderim backend'de operationId ile tekilleşir.
//  - Sıra garantisi: aynı aggregate'in işlemleri seq sırasına göre BİRER BİRER gönderilir.
//    Bir aggregate için aynı anda en fazla bir işlem "uçuşta" olur.
//  - Farklı aggregate'ler paralel gönderilebilir.

import type { OutboxEntry } from './types';

export const DEFAULT_MAX_ATTEMPTS = 5;

/** Bir aggregate için bir sonraki seq değeri (mevcut en yüksek + 1, yoksa 1). */
export function nextSeq(entries: ReadonlyArray<OutboxEntry>, aggregateId: string): number {
  let max = 0;
  for (const e of entries) {
    if (e.aggregateId === aggregateId && e.seq > max) max = e.seq;
  }
  return max + 1;
}

/**
 * Şu an gönderilmeye HAZIR işlemler. Aggregate başına yalnızca en düşük seq'li
 * senkronlanmamış işlem aday olur (sıralı garantisi); "sending" uçuşta sayılır ve atlanır;
 * "failed" ancak deneme hakkı kaldıysa yeniden denenir. Sonuç createdAt/seq ile kararlı sıralı.
 */
export function readyToSend(
  entries: ReadonlyArray<OutboxEntry>,
  maxAttempts: number = DEFAULT_MAX_ATTEMPTS,
): OutboxEntry[] {
  const byAggregate = new Map<string, OutboxEntry[]>();
  for (const e of entries) {
    const list = byAggregate.get(e.aggregateId) ?? [];
    list.push(e);
    byAggregate.set(e.aggregateId, list);
  }

  const ready: OutboxEntry[] = [];
  for (const group of byAggregate.values()) {
    const sorted = [...group].sort((a, b) => a.seq - b.seq);
    const head = sorted.find((e) => e.status !== 'synced');
    if (!head) continue; // aggregate tamamen senkron
    if (head.status === 'pending') {
      ready.push(head);
    } else if (head.status === 'failed' && head.attempts < maxAttempts) {
      ready.push(head);
    }
    // 'sending' → uçuşta, bekle
  }

  return ready.sort((a, b) => {
    if (a.createdAt < b.createdAt) return -1;
    if (a.createdAt > b.createdAt) return 1;
    return a.seq - b.seq;
  });
}

/** Bekleyen (henüz senkronlanmamış) toplam işlem sayısı — UI rozeti için. */
export function pendingCount(entries: ReadonlyArray<OutboxEntry>): number {
  return entries.filter((e) => e.status !== 'synced').length;
}

/** Kalıcı olarak başarısız (deneme hakkı bitmiş) işlemler — kullanıcı müdahalesi gerekir. */
export function deadLettered(
  entries: ReadonlyArray<OutboxEntry>,
  maxAttempts: number = DEFAULT_MAX_ATTEMPTS,
): OutboxEntry[] {
  return entries.filter((e) => e.status === 'failed' && e.attempts >= maxAttempts);
}

export function markSending(entry: OutboxEntry): OutboxEntry {
  return { ...entry, status: 'sending' };
}

export function markSynced(entry: OutboxEntry, syncedAt: string): OutboxEntry {
  return { ...entry, status: 'synced', syncedAt, lastError: null };
}

export function markFailed(entry: OutboxEntry, error: string): OutboxEntry {
  return { ...entry, status: 'failed', attempts: entry.attempts + 1, lastError: error };
}

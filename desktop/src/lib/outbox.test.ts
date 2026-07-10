import { describe, expect, it } from 'vitest';
import {
  deadLettered,
  markFailed,
  markSynced,
  nextSeq,
  pendingCount,
  readyToSend,
} from './outbox';
import type { OutboxEntry } from './types';

function entry(partial: Partial<OutboxEntry>): OutboxEntry {
  return {
    operationId: partial.operationId ?? 'op',
    aggregateType: 'sale',
    aggregateId: partial.aggregateId ?? 'agg',
    seq: partial.seq ?? 1,
    opType: 'create-sale',
    payload: {},
    status: partial.status ?? 'pending',
    attempts: partial.attempts ?? 0,
    lastError: partial.lastError ?? null,
    createdAt: partial.createdAt ?? '2026-07-10T10:00:00.000Z',
    syncedAt: partial.syncedAt ?? null,
    ...partial,
  };
}

describe('nextSeq', () => {
  it('yeni aggregate için 1', () => {
    expect(nextSeq([], 'a')).toBe(1);
  });
  it('mevcut en yüksek + 1', () => {
    const es = [entry({ aggregateId: 'a', seq: 1 }), entry({ aggregateId: 'a', seq: 3 })];
    expect(nextSeq(es, 'a')).toBe(4);
    expect(nextSeq(es, 'b')).toBe(1);
  });
});

describe('readyToSend — per-aggregate sıralı garanti', () => {
  it('aggregate başına yalnız en düşük seq senkronlanmamış işlemi verir', () => {
    const es = [
      entry({ operationId: 'a1', aggregateId: 'a', seq: 1, status: 'synced' }),
      entry({ operationId: 'a2', aggregateId: 'a', seq: 2, status: 'pending' }),
      entry({ operationId: 'a3', aggregateId: 'a', seq: 3, status: 'pending' }),
    ];
    const ready = readyToSend(es);
    expect(ready.map((e) => e.operationId)).toEqual(['a2']);
  });

  it('farklı aggregate\'ler paralel gönderilebilir', () => {
    const es = [
      entry({ operationId: 'a1', aggregateId: 'a', seq: 1, createdAt: '2026-07-10T10:00:00.000Z' }),
      entry({ operationId: 'b1', aggregateId: 'b', seq: 1, createdAt: '2026-07-10T10:00:01.000Z' }),
    ];
    const ready = readyToSend(es);
    expect(ready.map((e) => e.operationId)).toEqual(['a1', 'b1']);
  });

  it('uçuştaki (sending) işlem yeni gönderim engellenir', () => {
    const es = [
      entry({ operationId: 'a1', aggregateId: 'a', seq: 1, status: 'sending' }),
      entry({ operationId: 'a2', aggregateId: 'a', seq: 2, status: 'pending' }),
    ];
    expect(readyToSend(es)).toEqual([]);
  });

  it('deneme hakkı biten failed işlem gönderilmez', () => {
    const es = [entry({ aggregateId: 'a', seq: 1, status: 'failed', attempts: 5 })];
    expect(readyToSend(es, 5)).toEqual([]);
  });

  it('deneme hakkı olan failed işlem yeniden denenir', () => {
    const es = [entry({ operationId: 'a1', aggregateId: 'a', seq: 1, status: 'failed', attempts: 2 })];
    expect(readyToSend(es, 5).map((e) => e.operationId)).toEqual(['a1']);
  });

  it('tamamen senkron aggregate atlanır', () => {
    const es = [entry({ aggregateId: 'a', seq: 1, status: 'synced' })];
    expect(readyToSend(es)).toEqual([]);
  });

  it('createdAt sonra seq ile kararlı sıralar', () => {
    const es = [
      entry({ operationId: 'b', aggregateId: 'b', seq: 1, createdAt: '2026-07-10T10:00:02.000Z' }),
      entry({ operationId: 'a', aggregateId: 'a', seq: 1, createdAt: '2026-07-10T10:00:01.000Z' }),
    ];
    expect(readyToSend(es).map((e) => e.operationId)).toEqual(['a', 'b']);
  });
});

describe('durum geçişleri', () => {
  it('markFailed attempts artırır', () => {
    const e = markFailed(entry({ attempts: 1 }), 'ağ hatası');
    expect(e.status).toBe('failed');
    expect(e.attempts).toBe(2);
    expect(e.lastError).toBe('ağ hatası');
  });
  it('markSynced hatayı temizler', () => {
    const e = markSynced(entry({ status: 'failed', lastError: 'x' }), '2026-07-10T11:00:00.000Z');
    expect(e.status).toBe('synced');
    expect(e.lastError).toBeNull();
    expect(e.syncedAt).toBe('2026-07-10T11:00:00.000Z');
  });
});

describe('pendingCount / deadLettered', () => {
  it('senkron olmayanları sayar', () => {
    const es = [
      entry({ status: 'synced' }),
      entry({ status: 'pending', aggregateId: 'b' }),
      entry({ status: 'failed', aggregateId: 'c' }),
    ];
    expect(pendingCount(es)).toBe(2);
  });
  it('deneme hakkı biten failed dead-letter', () => {
    const es = [
      entry({ status: 'failed', attempts: 5, aggregateId: 'a' }),
      entry({ status: 'failed', attempts: 1, aggregateId: 'b' }),
    ];
    expect(deadLettered(es, 5).map((e) => e.aggregateId)).toEqual(['a']);
  });
});

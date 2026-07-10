import { useCallback, useEffect, useRef, useState } from 'react';
import { gatewayApi } from '../lib/api';
import {
  createSyncStore,
  getDb,
  listCachedParties,
  listCachedProducts,
  listLocalSales,
  type LocalDb,
  type SaleListItem,
} from '../lib/db';
import { pendingCount } from '../lib/outbox';
import { isOnline, runSync, type SyncSummary } from '../lib/sync';
import type { CachedParty, CachedProduct } from '../lib/types';

export interface TerminalState {
  db: LocalDb | null;
  online: boolean;
  syncing: boolean;
  pending: number;
  lastSync: SyncSummary | null;
  sales: SaleListItem[];
  products: CachedProduct[];
  parties: CachedParty[];
  refresh: () => Promise<void>;
  sync: () => Promise<void>;
}

export function useTerminal(): TerminalState {
  const [db, setDb] = useState<LocalDb | null>(null);
  const [online, setOnline] = useState(isOnline());
  const [syncing, setSyncing] = useState(false);
  const [pending, setPending] = useState(0);
  const [lastSync, setLastSync] = useState<SyncSummary | null>(null);
  const [sales, setSales] = useState<SaleListItem[]>([]);
  const [products, setProducts] = useState<CachedProduct[]>([]);
  const [parties, setParties] = useState<CachedParty[]>([]);
  const syncingRef = useRef(false);

  useEffect(() => {
    getDb()
      .then(setDb)
      .catch((e) => console.error('Yerel veritabanı yüklenemedi', e));
  }, []);

  const refresh = useCallback(async () => {
    if (!db) return;
    const store = createSyncStore(db);
    const [salesList, prods, prts, outbox] = await Promise.all([
      listLocalSales(db),
      listCachedProducts(db),
      listCachedParties(db),
      store.loadOutbox(),
    ]);
    setSales(salesList);
    setProducts(prods);
    setParties(prts);
    setPending(pendingCount(outbox));
  }, [db]);

  const sync = useCallback(async () => {
    if (!db || syncingRef.current || !isOnline()) return;
    syncingRef.current = true;
    setSyncing(true);
    try {
      const summary = await runSync(createSyncStore(db), gatewayApi);
      setLastSync(summary);
      await refresh();
    } finally {
      syncingRef.current = false;
      setSyncing(false);
    }
  }, [db, refresh]);

  // İlk yükte veriyi getir, online ise senkronla.
  useEffect(() => {
    if (!db) return;
    void refresh().then(() => {
      if (isOnline()) void sync();
    });
  }, [db, refresh, sync]);

  // Online/offline değişimini dinle; online olunca otomatik senkronla.
  useEffect(() => {
    const goOnline = () => {
      setOnline(true);
      void sync();
    };
    const goOffline = () => setOnline(false);
    window.addEventListener('online', goOnline);
    window.addEventListener('offline', goOffline);
    return () => {
      window.removeEventListener('online', goOnline);
      window.removeEventListener('offline', goOffline);
    };
  }, [sync]);

  return { db, online, syncing, pending, lastSync, sales, products, parties, refresh, sync };
}

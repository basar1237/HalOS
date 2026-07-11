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

  // Online ise ana veriyi (alıcı/müstahsil/ürün/son satışlar) doğrudan API'den çek.
  // Yerel SQLite cache boş olsa da (ör. tarayıcı modu) terminal kullanılabilir kalır.
  useEffect(() => {
    if (!online) return;
    let cancelled = false;
    void (async () => {
      try {
        const [prts, prods, rawSales] = await Promise.all([
          gatewayApi.pullParties(),
          gatewayApi.pullProducts(),
          gatewayApi.pullSales(),
        ]);
        if (cancelled) return;
        setParties(prts);
        setProducts(prods);
        const nameOf = new Map(prts.map((p) => [p.id, p.name]));
        const statusLabel: Record<number, string> = { 1: 'draft', 2: 'completed', 3: 'cancelled' };
        setSales(
          rawSales.map((s) => ({
            operationId: s.id,
            serverId: s.id,
            partyName: nameOf.get(s.buyerPartyId) ?? '—',
            grossTotal: s.grossAmount,
            saleTerm: s.term,
            status: statusLabel[s.status] ?? 'completed',
            syncStatus: 'synced',
            createdAt: s.soldAt,
          })),
        );
      } catch (e) {
        console.warn('API ana veri çekilemedi (offline cache kullanılacak):', e);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [online]);

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

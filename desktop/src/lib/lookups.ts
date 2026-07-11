// Taraf/ürün/depo ID'lerini isme çeviren ortak hook (mikroservis: kayıtlar yalnız ID tutar).
import { useEffect, useState } from 'react';
import { apiGet } from './api';

interface PartyRow { id: string; displayName?: string; name?: string }
interface ProductRow { id: string; name: string }
interface WarehouseRow { id: string; name: string }
interface Paged<T> { items?: T[] }

export interface Lookups {
  partyName: (id: string) => string;
  productName: (id: string) => string;
  warehouseName: (id: string) => string;
  loaded: boolean;
}

export function useLookups(): Lookups {
  const [party, setParty] = useState<Map<string, string>>(new Map());
  const [product, setProduct] = useState<Map<string, string>>(new Map());
  const [warehouse, setWarehouse] = useState<Map<string, string>>(new Map());
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      try {
        const [p, pr, wh] = await Promise.all([
          apiGet<Paged<PartyRow>>('/api/party/parties?page=1&pageSize=500'),
          apiGet<Paged<ProductRow>>('/api/inventory/products?page=1&pageSize=500'),
          apiGet<WarehouseRow[]>('/api/inventory/warehouses'),
        ]);
        if (cancelled) return;
        setParty(new Map((p.items ?? []).map((x) => [x.id, x.displayName ?? x.name ?? ''])));
        setProduct(new Map((pr.items ?? []).map((x) => [x.id, x.name])));
        setWarehouse(new Map((Array.isArray(wh) ? wh : []).map((x) => [x.id, x.name])));
      } catch {
        /* offline / hata → ID kısaltması gösterilir */
      } finally {
        if (!cancelled) setLoaded(true);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const short = (id: string) => (id ? id.slice(0, 8) : '—');
  return {
    partyName: (id) => party.get(id) || short(id),
    productName: (id) => product.get(id) || short(id),
    warehouseName: (id) => warehouse.get(id) || short(id),
    loaded,
  };
}

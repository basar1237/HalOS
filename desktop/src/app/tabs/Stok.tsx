import { useLookups } from '../../lib/lookups';
import { useFetch } from '../../lib/useFetch';

interface StockItem { id: string; productId: string; warehouseId: string; quantityOnHand: number; reorderThreshold?: number | null }
interface Paged<T> { items?: T[] }
const QTY = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 3 });

export function Stok() {
  const { productName, warehouseName } = useLookups();
  const { data, error } = useFetch<Paged<StockItem>>('/api/inventory/stock?page=1&pageSize=200');
  const rows = data?.items ?? [];

  return (
    <section className="panel">
      <h2>Stok &amp; Depo ({rows.length})</h2>
      {error && <p className="error">{error}</p>}
      {rows.length === 0 ? (
        <p className="muted">Stok kaydı yok.</p>
      ) : (
        <table>
          <thead>
            <tr><th>Ürün</th><th>Depo</th><th className="num">Kalan</th><th>Durum</th></tr>
          </thead>
          <tbody>
            {rows.map((s) => {
              const low = s.reorderThreshold != null && s.quantityOnHand <= s.reorderThreshold;
              return (
                <tr key={s.id}>
                  <td>{productName(s.productId)}</td>
                  <td>{warehouseName(s.warehouseId)}</td>
                  <td className="num">{QTY.format(s.quantityOnHand)}</td>
                  <td>{low ? <span className="tag conflict">Eşik altı</span> : <span className="muted">Normal</span>}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </section>
  );
}

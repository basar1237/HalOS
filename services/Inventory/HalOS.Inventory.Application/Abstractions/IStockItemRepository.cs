using HalOS.Inventory.Application.Contracts;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Abstractions;

/// <summary>
/// StockItem aggregate persistence port'u. Tüm sorgular tenant global query filter'a tabidir (BK-8).
/// Hareketler (StockMovement) aggregate ile birlikte yüklenir; kalan türetildiğinden (docs/02 §115)
/// hareket koleksiyonu iş metotları için gereklidir. Finance.ICurrentAccountRepository deseniyle birebir.
/// </summary>
public interface IStockItemRepository
{
    /// <summary>Stok kalemini hareketleriyle birlikte getirir (tenant filtreli).</summary>
    Task<StockItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir ürünün stok kalemini hareketleriyle getirir; yoksa null. Tenant + ürün başına
    /// tek stok kalemi (UNIQUE(tenant_id, product_id)).
    /// </summary>
    Task<StockItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant filtreli, sayfalanmış stok kalemi listesi. Hareketler dahil (kalan türetimi için).
    /// </summary>
    Task<PagedResult<StockItem>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(StockItem item);

    void Update(StockItem item);
}

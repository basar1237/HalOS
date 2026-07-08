using HalOS.Inventory.Application.Contracts;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Abstractions;

/// <summary>
/// StockItem aggregate persistence port'u. Tüm sorgular tenant global query filter'a tabidir (BK-8).
/// Hareketler (StockMovement) aggregate ile birlikte yüklenir; kalan türetildiğinden (docs/02 §115)
/// hareket koleksiyonu iş metotları için gereklidir. Stok kalemi artık (tenant, depo, ürün) bazlıdır
/// (docs/06 S2.1). Finance.ICurrentAccountRepository deseniyle birebir.
/// </summary>
public interface IStockItemRepository
{
    /// <summary>Stok kalemini hareketleriyle birlikte getirir (tenant filtreli).</summary>
    Task<StockItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir depodaki belirli ürünün stok kalemini hareketleriyle getirir; yoksa null.
    /// (tenant, depo, ürün) başına tek stok kalemi (UNIQUE(tenant_id, warehouse_id, product_id)).
    /// </summary>
    Task<StockItem?> GetByWarehouseAndProductAsync(
        Guid warehouseId,
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir ürünün stok kalemlerini (tüm depolarda) hareketleriyle getirir. Fire kaydı gibi depo
    /// belirtilmeyen işlemler için kullanılır; ürün birden çok depoda olabilir.
    /// </summary>
    Task<IReadOnlyList<StockItem>> ListByProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant filtreli, sayfalanmış stok kalemi listesi. Hareketler dahil (kalan türetimi için).
    /// </summary>
    Task<PagedResult<StockItem>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Yeniden-sipariş eşiği tanımlı ve eldeki miktarı eşiğe eşit veya altında olan stok kalemlerini
    /// getirir (docs/06 S2.1 stok uyarıları — düşük stok listesi). Hareketler dahil (kalan türetimi).
    /// </summary>
    Task<IReadOnlyList<StockItem>> ListLowStockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen aralıkta ürün bazlı fire analizi (docs/06 S2.1 detaylı fire analizi): her ürün için
    /// toplam giriş, toplam fire ve fire oranı (%). StockMovement'lardan AsNoTracking Kind-bazlı
    /// agregasyon; tenant global filter otomatik (BK-8). Yeni tablo YOK.
    /// </summary>
    Task<SpoilageAnalysisReportDto> GetSpoilageAnalysisAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    void Add(StockItem item);

    void Update(StockItem item);
}

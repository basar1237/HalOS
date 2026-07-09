using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Abstractions;

/// <summary>
/// Product aggregate persistence port'u (docs/03 M2 / docs/05 §3.3). Tüm sorgular tenant global query
/// filter'a tabidir (BK-8). IWarehouseRepository deseniyle birebir.
/// </summary>
public interface IProductRepository
{
    /// <summary>Ürünü kimliğiyle getirir (tenant filtreli); yoksa null.</summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tenant'ın ürünlerini ada göre sıralı sayfalı listeler. onlyActive=true ise yalnız aktif.</summary>
    Task<(IReadOnlyList<Product> Items, long TotalCount)> ListAsync(
        int page,
        int pageSize,
        bool onlyActive,
        CancellationToken cancellationToken = default);

    void Add(Product product);

    void Update(Product product);
}

using HalOS.Sales.Application.Contracts;
using HalOS.Sales.Domain.Aggregates;

namespace HalOS.Sales.Application.Abstractions;

/// <summary>
/// SaleTransaction aggregate persistence port'u. Tüm sorgular tenant global query filter'a
/// tabidir (BK-8). Satış satırları, komisyon/kesinti/hakediş bağlı entity'leriyle birlikte yüklenir.
/// </summary>
public interface ISaleTransactionRepository
{
    /// <summary>Satışı satırları/kesinti/hakedişiyle birlikte getirir.</summary>
    Task<SaleTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Offline idempotency (docs/04 §5): verilen operationId ile satış zaten var mı. Aynı
    /// operationId ile ikinci CreateSale reddedilir/geri döner.
    /// </summary>
    Task<SaleTransaction?> GetByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant filtreli, sold_at'e göre azalan sıralı sayfalanmış satış listesi (docs/05 §6
    /// (tenant_id, sold_at) indeksi). Opsiyonel tarih aralığı filtresi.
    /// </summary>
    Task<PagedResult<SaleTransaction>> ListAsync(
        int page,
        int pageSize,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    void Add(SaleTransaction sale);

    void Update(SaleTransaction sale);
}

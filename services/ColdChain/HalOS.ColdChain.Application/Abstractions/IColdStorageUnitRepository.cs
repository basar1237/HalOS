using HalOS.ColdChain.Application.Contracts;
using HalOS.ColdChain.Domain.Aggregates;

namespace HalOS.ColdChain.Application.Abstractions;

/// <summary>
/// ColdStorageUnit aggregate persistence port'u. Tüm sorgular tenant global query filter'a tabidir
/// (BK-8). Okumalar (SensorReading) aggregate ile birlikte yüklenir (idempotency kontrolü + son okuma
/// türetimi için). Inventory StockItemRepository deseniyle birebir.
/// </summary>
public interface IColdStorageUnitRepository
{
    /// <summary>Depoyu okumalarıyla birlikte getirir (idempotency + son okuma için).</summary>
    Task<ColdStorageUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tenant filtreli, ada göre sıralı sayfalanmış depo listesi.</summary>
    Task<PagedResult<ColdStorageUnit>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(ColdStorageUnit unit);

    void Update(ColdStorageUnit unit);
}

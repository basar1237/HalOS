using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;

namespace HalOS.Integration.Application.Abstractions;

/// <summary>
/// ProductPassport (künye) aggregate persistence port'u. Tüm sorgular tenant global query filter'a
/// tabidir (BK-8). <see cref="IProducerReceiptRepository"/>/<see cref="IInvoiceRepository"/> deseniyle birebir.
/// </summary>
public interface IProductPassportRepository
{
    /// <summary>Künyeyi getirir (tenant filtreli); yoksa null.</summary>
    Task<ProductPassport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir mal geliş kalemine ait künyeyi getirir (idempotency kontrolü — kalem başına en fazla bir
    /// künye); yoksa null.
    /// </summary>
    Task<ProductPassport?> GetByConsignmentItemIdAsync(Guid consignmentItemId, CancellationToken cancellationToken = default);

    /// <summary>Tenant filtreli, sayfalanmış künye listesi.</summary>
    Task<PagedResult<ProductPassport>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(ProductPassport passport);

    void Update(ProductPassport passport);
}

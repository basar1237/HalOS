using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;

namespace HalOS.Integration.Application.Abstractions;

/// <summary>
/// Invoice (e-Fatura HAL) aggregate persistence port'u. Tüm sorgular tenant global query filter'a
/// tabidir (BK-8). <see cref="IProducerReceiptRepository"/> deseniyle birebir.
/// </summary>
public interface IInvoiceRepository
{
    /// <summary>e-Fatura'yı getirir (tenant filtreli); yoksa null.</summary>
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir satışa ait e-Fatura'yı getirir (idempotency kontrolü — bir satış tenant içinde en fazla bir
    /// e-Fatura üretir); yoksa null.
    /// </summary>
    Task<Invoice?> GetBySaleTransactionIdAsync(Guid saleTransactionId, CancellationToken cancellationToken = default);

    /// <summary>Tenant filtreli, sayfalanmış e-Fatura listesi.</summary>
    Task<PagedResult<Invoice>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Bekleyen (Draft/Failed) e-Fatura adedi — dashboard "Bekleyen e-Belge". Tenant filtreli (BK-8).</summary>
    Task<long> CountPendingAsync(CancellationToken cancellationToken = default);

    void Add(Invoice invoice);

    void Update(Invoice invoice);
}

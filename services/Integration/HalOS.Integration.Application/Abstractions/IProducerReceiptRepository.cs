using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;

namespace HalOS.Integration.Application.Abstractions;

/// <summary>
/// ProducerReceipt (e-MM) aggregate persistence port'u. Tüm sorgular tenant global query filter'a
/// tabidir (BK-8). Kesinti kalemleri (ReceiptDeduction) aggregate ile birlikte yüklenir.
/// Finance.ICurrentAccountRepository deseniyle birebir.
/// </summary>
public interface IProducerReceiptRepository
{
    /// <summary>e-MM'i kesinti kalemleriyle birlikte getirir (tenant filtreli); yoksa null.</summary>
    Task<ProducerReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir satışa ait e-MM'i getirir (idempotency kontrolü — bir satış tenant içinde en fazla bir
    /// e-MM üretir); yoksa null.
    /// </summary>
    Task<ProducerReceipt?> GetBySaleTransactionIdAsync(Guid saleTransactionId, CancellationToken cancellationToken = default);

    /// <summary>Tenant filtreli, sayfalanmış e-MM listesi. Kesinti kalemleri dahil.</summary>
    Task<PagedResult<ProducerReceipt>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(ProducerReceipt receipt);

    void Update(ProducerReceipt receipt);
}

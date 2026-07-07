using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;

namespace HalOS.Integration.Application.Abstractions;

/// <summary>
/// HksNotification (HKS bildirimi) aggregate persistence port'u. Tüm sorgular tenant global query
/// filter'a tabidir (BK-8). <see cref="IProducerReceiptRepository"/> deseniyle birebir.
/// </summary>
public interface IHksNotificationRepository
{
    /// <summary>HKS bildirimini getirir (tenant filtreli); yoksa null.</summary>
    Task<HksNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir satışa ait HKS bildirimini getirir (idempotency kontrolü — bir satış tenant içinde en fazla
    /// bir bildirim üretir); yoksa null.
    /// </summary>
    Task<HksNotification?> GetBySaleTransactionIdAsync(Guid saleTransactionId, CancellationToken cancellationToken = default);

    /// <summary>Tenant filtreli, sayfalanmış HKS bildirimi listesi.</summary>
    Task<PagedResult<HksNotification>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(HksNotification notification);

    void Update(HksNotification notification);
}

using HalOS.Identity.Domain.Aggregates;

namespace HalOS.Identity.Application.Abstractions;

/// <summary>Subscription aggregate persistence port'u.</summary>
public interface ISubscriptionRepository
{
    Task<Subscription?> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    void Add(Subscription subscription);
}

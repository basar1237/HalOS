using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Identity.Infrastructure.Persistence.Repositories;

internal sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly IdentityDbContext _dbContext;

    public SubscriptionRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Subscription?> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

    public void Add(Subscription subscription) => _dbContext.Subscriptions.Add(subscription);
}

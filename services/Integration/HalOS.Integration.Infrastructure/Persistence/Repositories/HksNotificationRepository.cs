using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Integration.Infrastructure.Persistence.Repositories;

/// <summary>
/// HksNotification (HKS bildirimi) aggregate persistence adaptörü. Tüm sorgular tenant global query
/// filter'a tabidir (BK-8). Idempotency ön-kontrolü (<see cref="GetBySaleTransactionIdAsync"/>) + DB
/// UNIQUE(tenant_id, sale_transaction_id) birlikte. ProducerReceiptRepository deseniyle birebir.
/// </summary>
internal sealed class HksNotificationRepository : IHksNotificationRepository
{
    private readonly IntegrationDbContext _dbContext;

    public HksNotificationRepository(IntegrationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<HksNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.HksNotifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public Task<HksNotification?> GetBySaleTransactionIdAsync(Guid saleTransactionId, CancellationToken cancellationToken = default) =>
        _dbContext.HksNotifications
            .FirstOrDefaultAsync(n => n.SaleTransactionId == saleTransactionId, cancellationToken);

    public async Task<PagedResult<HksNotification>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.HksNotifications
            .AsNoTracking()
            .AsQueryable();

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.NotifiedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<HksNotification>(items, page, pageSize, totalCount);
    }

    public Task<long> CountPendingAsync(CancellationToken cancellationToken = default) =>
        _dbContext.HksNotifications
            .AsNoTracking()
            .Where(n => n.Status == HksNotificationStatus.Draft || n.Status == HksNotificationStatus.Failed)
            .LongCountAsync(cancellationToken);

    public void Add(HksNotification notification) => _dbContext.HksNotifications.Add(notification);

    public void Update(HksNotification notification) => _dbContext.HksNotifications.Update(notification);
}

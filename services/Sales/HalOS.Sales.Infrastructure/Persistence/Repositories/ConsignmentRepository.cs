using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Sales.Infrastructure.Persistence.Repositories;

internal sealed class ConsignmentRepository : IConsignmentRepository
{
    private readonly SalesDbContext _dbContext;

    public ConsignmentRepository(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Consignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Consignments
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<long> CountReceivedOnAsync(DateTime day, CancellationToken cancellationToken = default)
    {
        // Gün [start, ertesi gün) yarı-açık aralığı (tarih bileşeni). Tenant global filter otomatik (BK-8).
        var start = day.Date;
        var end = start.AddDays(1);
        return _dbContext.Consignments
            .AsNoTracking()
            .Where(c => c.ReceivedAt >= start && c.ReceivedAt < end)
            .LongCountAsync(cancellationToken);
    }

    public void Add(Consignment consignment) => _dbContext.Consignments.Add(consignment);
}

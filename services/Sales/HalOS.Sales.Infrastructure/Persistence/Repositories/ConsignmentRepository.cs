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

    public void Add(Consignment consignment) => _dbContext.Consignments.Add(consignment);
}

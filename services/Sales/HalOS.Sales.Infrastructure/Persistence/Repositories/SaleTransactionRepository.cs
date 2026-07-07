using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Contracts;
using HalOS.Sales.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Sales.Infrastructure.Persistence.Repositories;

internal sealed class SaleTransactionRepository : ISaleTransactionRepository
{
    private readonly SalesDbContext _dbContext;

    public SaleTransactionRepository(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SaleTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.SaleTransactions
            .Include(s => s.Lines)
            .Include(s => s.Deductions)
            .Include(s => s.CommissionCalculation)
            .Include(s => s.Settlement)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<SaleTransaction?> GetByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) =>
        _dbContext.SaleTransactions
            .Include(s => s.Lines)
            .Include(s => s.Deductions)
            .Include(s => s.CommissionCalculation)
            .Include(s => s.Settlement)
            .FirstOrDefaultAsync(s => s.OperationId == operationId, cancellationToken);

    public async Task<PagedResult<SaleTransaction>> ListAsync(
        int page,
        int pageSize,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SaleTransactions
            .AsNoTracking()
            .Include(s => s.Lines)
            .Include(s => s.Deductions)
            .Include(s => s.CommissionCalculation)
            .Include(s => s.Settlement)
            .AsQueryable();

        if (from is not null)
        {
            query = query.Where(s => s.SoldAt >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(s => s.SoldAt <= to.Value);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.SoldAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SaleTransaction>(items, page, pageSize, totalCount);
    }

    public void Add(SaleTransaction sale) => _dbContext.SaleTransactions.Add(sale);

    public void Update(SaleTransaction sale) => _dbContext.SaleTransactions.Update(sale);
}

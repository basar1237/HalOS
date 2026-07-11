using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Contracts;
using HalOS.Finance.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Finance.Infrastructure.Persistence.Repositories;

internal sealed class ChequeRepository : IChequeRepository
{
    private readonly FinanceDbContext _dbContext;

    public ChequeRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Cheque cheque) => _dbContext.Cheques.Add(cheque);

    public void Update(Cheque cheque) => _dbContext.Cheques.Update(cheque);

    public Task<Cheque?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Cheques.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<PagedResult<Cheque>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Cheques.AsNoTracking();
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Cheque>(items, page, pageSize, total);
    }
}

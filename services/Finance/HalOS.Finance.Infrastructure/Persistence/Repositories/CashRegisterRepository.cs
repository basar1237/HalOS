using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Finance.Infrastructure.Persistence.Repositories;

internal sealed class CashRegisterRepository : ICashRegisterRepository
{
    private readonly FinanceDbContext _dbContext;

    public CashRegisterRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(CashRegister register) => _dbContext.CashRegisters.Add(register);

    public Task<CashRegister?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CashRegisters
            .Include(r => r.Movements)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CashRegister>> ListAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.CashRegisters
            .AsNoTracking()
            .Include(r => r.Movements)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

    public void RegisterNew(object child) => _dbContext.Entry(child).State = EntityState.Added;
}

using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Contracts;
using HalOS.Finance.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Finance.Infrastructure.Persistence.Repositories;

/// <summary>
/// CurrentAccount aggregate persistence adaptörü. Tüm sorgular tenant global query filter'a
/// tabidir (BK-8). Hareketler (AccountEntry) aggregate ile birlikte yüklenir çünkü bakiye
/// türetilir (docs/02 §3.4) ve iş metotları hareket koleksiyonuna ihtiyaç duyar.
/// </summary>
internal sealed class CurrentAccountRepository : ICurrentAccountRepository
{
    private readonly FinanceDbContext _dbContext;

    public CurrentAccountRepository(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CurrentAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CurrentAccounts
            .Include(a => a.Entries)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<CurrentAccount?> GetByPartyIdAsync(Guid partyId, CancellationToken cancellationToken = default) =>
        _dbContext.CurrentAccounts
            .Include(a => a.Entries)
            .FirstOrDefaultAsync(a => a.PartyId == partyId, cancellationToken);

    public async Task<PagedResult<CurrentAccount>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CurrentAccounts
            .AsNoTracking()
            .Include(a => a.Entries)
            .AsQueryable();

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderBy(a => a.PartyId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CurrentAccount>(items, page, pageSize, totalCount);
    }

    public void Add(CurrentAccount account) => _dbContext.CurrentAccounts.Add(account);

    public void Update(CurrentAccount account) => _dbContext.CurrentAccounts.Update(account);
}

using HalOS.Party.Application.Abstractions;
using HalOS.Party.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using PartyAggregate = HalOS.Party.Domain.Aggregates.Party;

namespace HalOS.Party.Infrastructure.Persistence.Repositories;

internal sealed class PartyRepository : IPartyRepository
{
    private readonly PartyDbContext _dbContext;

    public PartyRepository(PartyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PartyAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Parties
            .Include(p => p.Roles)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsByTcknAsync(string tckn, CancellationToken cancellationToken = default) =>
        _dbContext.Parties.AnyAsync(p => p.Tckn == tckn, cancellationToken);

    public Task<bool> ExistsByVknAsync(string vkn, CancellationToken cancellationToken = default) =>
        _dbContext.Parties.AnyAsync(p => p.Vkn == vkn, cancellationToken);

    public async Task<PagedResult<PartyAggregate>> ListAsync(
        int page,
        int pageSize,
        bool onlyActive,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Parties
            .AsNoTracking()
            .Include(p => p.Roles)
            .AsQueryable();

        if (onlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PartyAggregate>(items, page, pageSize, totalCount);
    }

    public void Add(PartyAggregate party) => _dbContext.Parties.Add(party);

    public void Update(PartyAggregate party) => _dbContext.Parties.Update(party);
}

using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Application.Contracts;
using HalOS.ColdChain.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.ColdChain.Infrastructure.Persistence.Repositories;

/// <summary>
/// ColdStorageUnit aggregate persistence adaptörü. Tüm sorgular tenant global query filter'a tabidir
/// (BK-8). Okumalar (SensorReading) aggregate ile birlikte yüklenir (idempotency + son okuma türetimi
/// için). Inventory StockItemRepository deseniyle birebir.
/// </summary>
internal sealed class ColdStorageUnitRepository : IColdStorageUnitRepository
{
    private readonly ColdChainDbContext _dbContext;

    public ColdStorageUnitRepository(ColdChainDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ColdStorageUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.ColdStorageUnits
            .Include(u => u.Readings)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<PagedResult<ColdStorageUnit>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ColdStorageUnits
            .AsNoTracking()
            .Include(u => u.Readings)
            .AsQueryable();

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ColdStorageUnit>(items, page, pageSize, totalCount);
    }

    public void Add(ColdStorageUnit unit) => _dbContext.ColdStorageUnits.Add(unit);

    public void Update(ColdStorageUnit unit) => _dbContext.ColdStorageUnits.Update(unit);
}

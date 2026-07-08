using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Integration.Infrastructure.Persistence.Repositories;

/// <summary>
/// ProductPassport (künye) aggregate persistence adaptörü. Tüm sorgular tenant global query filter'a
/// tabidir (BK-8). ProducerReceipt/Invoice repository deseniyle birebir. Idempotency ön-kontrolü
/// (<see cref="GetByConsignmentItemIdAsync"/>) + DB UNIQUE(tenant_id, consignment_item_id); eşzamanlı
/// teslimatta ikinci SaveChanges DbUpdateException verir, MassTransit retry'ında ön-kontrol mevcut
/// künyeyi bulup atlar (idempotent).
/// </summary>
internal sealed class ProductPassportRepository : IProductPassportRepository
{
    private readonly IntegrationDbContext _dbContext;

    public ProductPassportRepository(IntegrationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductPassport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.ProductPassports.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<ProductPassport?> GetByConsignmentItemIdAsync(Guid consignmentItemId, CancellationToken cancellationToken = default) =>
        _dbContext.ProductPassports.FirstOrDefaultAsync(p => p.ConsignmentItemId == consignmentItemId, cancellationToken);

    public async Task<PagedResult<ProductPassport>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProductPassports.AsNoTracking().AsQueryable();

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductPassport>(items, page, pageSize, totalCount);
    }

    public void Add(ProductPassport passport) => _dbContext.ProductPassports.Add(passport);

    public void Update(ProductPassport passport) => _dbContext.ProductPassports.Update(passport);
}

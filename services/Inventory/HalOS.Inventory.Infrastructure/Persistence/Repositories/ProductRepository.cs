using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Inventory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Product aggregate persistence adaptörü (docs/03 M2 / docs/05 §3.3). Tüm sorgular tenant global
/// query filter'a tabidir (BK-8). WarehouseRepository deseniyle birebir.
/// </summary>
internal sealed class ProductRepository : IProductRepository
{
    private readonly InventoryDbContext _dbContext;

    public ProductRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, long TotalCount)> ListAsync(
        int page,
        int pageSize,
        bool onlyActive,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking();
        if (onlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Add(Product product) => _dbContext.Products.Add(product);

    public void Update(Product product) => _dbContext.Products.Update(product);
}

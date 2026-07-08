using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;
using HalOS.Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Inventory.Infrastructure.Persistence.Repositories;

/// <summary>
/// StockItem aggregate persistence adaptörü. Tüm sorgular tenant global query filter'a tabidir (BK-8).
/// Hareketler (StockMovement) aggregate ile birlikte yüklenir çünkü kalan türetilir (docs/02 §115) ve
/// iş metotları (çıkış/fire BK-7 kontrolü) hareket koleksiyonuna ihtiyaç duyar. Finance
/// CurrentAccountRepository deseniyle birebir.
/// </summary>
internal sealed class StockItemRepository : IStockItemRepository
{
    private readonly InventoryDbContext _dbContext;

    public StockItemRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<StockItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.StockItems
            .Include(i => i.Movements)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<StockItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default) =>
        _dbContext.StockItems
            .Include(i => i.Movements)
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);

    public async Task<PagedResult<StockItem>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StockItems
            .AsNoTracking()
            .Include(i => i.Movements)
            .AsQueryable();

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.ProductId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockItem>(items, page, pageSize, totalCount);
    }

    public void Add(StockItem item) => _dbContext.StockItems.Add(item);

    public void Update(StockItem item) => _dbContext.StockItems.Update(item);
}

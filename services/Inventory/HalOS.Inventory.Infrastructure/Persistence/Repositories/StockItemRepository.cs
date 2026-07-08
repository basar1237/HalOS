using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;
using HalOS.Inventory.Domain.Aggregates;
using HalOS.Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Inventory.Infrastructure.Persistence.Repositories;

/// <summary>
/// StockItem aggregate persistence adaptörü. Tüm sorgular tenant global query filter'a tabidir (BK-8).
/// Hareketler (StockMovement) aggregate ile birlikte yüklenir çünkü kalan türetilir (docs/02 §115) ve
/// iş metotları (çıkış/fire BK-7 kontrolü) hareket koleksiyonuna ihtiyaç duyar. Stok kalemi artık
/// (tenant, depo, ürün) bazlıdır (docs/06 S2.1). Finance CurrentAccountRepository deseniyle birebir.
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

    public Task<StockItem?> GetByWarehouseAndProductAsync(
        Guid warehouseId,
        Guid productId,
        CancellationToken cancellationToken = default) =>
        _dbContext.StockItems
            .Include(i => i.Movements)
            .FirstOrDefaultAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId, cancellationToken);

    public async Task<IReadOnlyList<StockItem>> ListByProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.StockItems
            .Include(i => i.Movements)
            .Where(i => i.ProductId == productId)
            .ToListAsync(cancellationToken);

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

    public async Task<IReadOnlyList<StockItem>> ListLowStockAsync(CancellationToken cancellationToken = default)
    {
        // Eşiği tanımlı adayları hareketleriyle yükle; kalan (QuantityOnHand) türetilmiş olduğundan
        // (Σ hareket, kalıcı kolon değil) SQL'de filtrelenemez → eşik altı karşılaştırma bellekte
        // yapılır. Aday kümesi eşik tanımlı kalemlerle sınırlıdır (docs/06 S2.1). Tenant filter otomatik.
        var candidates = await _dbContext.StockItems
            .AsNoTracking()
            .Include(i => i.Movements)
            .Where(i => i.ReorderThreshold != null)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(i => i.QuantityOnHand <= i.ReorderThreshold!.Value)
            .OrderBy(i => i.ProductId)
            .ToList();
    }

    public async Task<SpoilageAnalysisReportDto> GetSpoilageAnalysisAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        // Ürün bazlı fire analizi (docs/06 S2.1): aralıktaki giriş (Intake) ve fire (Spoilage)
        // hareketlerini ürün üzerinden agregele. Hareket ürünü stok kalemi üzerinden bilinir; bu
        // yüzden stok_item ile join edilip product_id'ye göre gruplanır. AsNoTracking; tenant filter
        // otomatik (BK-8). İşaretli miktar çıkışta negatif olduğundan mutlak değerle toplanır.
        var movements =
            from m in _dbContext.StockMovements.AsNoTracking()
            join i in _dbContext.StockItems.AsNoTracking() on m.StockItemId equals i.Id
            where m.OccurredAt >= fromUtc && m.OccurredAt <= toUtc
                && (m.Kind == StockMovementKind.Intake || m.Kind == StockMovementKind.Spoilage)
            select new { i.ProductId, m.Kind, m.SignedQuantity };

        var grouped = await movements
            .GroupBy(x => x.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalIntake = g.Where(x => x.Kind == StockMovementKind.Intake).Sum(x => x.SignedQuantity),
                // Fire hareketleri negatif işaretli; mutlak değer için ters çevir (toplam ≤ 0 → ≥ 0).
                TotalSpoilage = -g.Where(x => x.Kind == StockMovementKind.Spoilage).Sum(x => x.SignedQuantity)
            })
            .ToListAsync(cancellationToken);

        var items = grouped
            .Select(g => new SpoilageAnalysisItemDto(
                g.ProductId,
                g.TotalIntake,
                g.TotalSpoilage,
                SpoilageRate(g.TotalIntake, g.TotalSpoilage)))
            .OrderByDescending(x => x.TotalIntake)
            .ThenBy(x => x.ProductId)
            .ToList();

        return new SpoilageAnalysisReportDto(fromUtc, toUtc, items);
    }

    /// <summary>Fire oranı yüzdesi = fire / giriş * 100; giriş 0 ise 0 (sıfıra bölme yok). decimal (BK-2).</summary>
    private static decimal SpoilageRate(decimal totalIntake, decimal totalSpoilage) =>
        totalIntake == 0m
            ? 0m
            : Math.Round(totalSpoilage / totalIntake * 100m, 2, MidpointRounding.AwayFromZero);

    public void Add(StockItem item) => _dbContext.StockItems.Add(item);

    public void Update(StockItem item) => _dbContext.StockItems.Update(item);
}

using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Inventory.Infrastructure.Persistence.Repositories;

/// <summary>
/// Warehouse aggregate persistence adaptörü (docs/06 S2.1 depo lokasyonu). Tüm sorgular tenant global
/// query filter'a tabidir (BK-8). Kod tenant içinde tekildir. StockItemRepository deseniyle birebir.
/// </summary>
internal sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly InventoryDbContext _dbContext;

    public WarehouseRepository(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Code == code, cancellationToken);

    // Deterministik sıralama (koda göre): tekillik değişmezi bozulsa (bozuk veri) bile hep aynı
    // varsayılan depo döner; olay-güdümlü giriş/çıkış ile okuma/komut handler'ları tutarlı depoya denk gelir.
    public Task<Warehouse?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Warehouses
            .OrderBy(w => w.Code)
            .FirstOrDefaultAsync(w => w.IsDefault, cancellationToken);

    public async Task<IReadOnlyList<Warehouse>> ListDefaultsAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Warehouses
            .Where(w => w.IsDefault)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.Warehouses.AnyAsync(w => w.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Warehouse>> ListAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

    public void Add(Warehouse warehouse) => _dbContext.Warehouses.Add(warehouse);

    public void Update(Warehouse warehouse) => _dbContext.Warehouses.Update(warehouse);
}

using HalOS.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HalOS.Inventory.Infrastructure.Persistence;

/// <summary>
/// Tasarım-zamanı (dotnet ef) DbContext üreticisi. Migration üretimi için gereklidir; çalışma
/// zamanında DI'daki gerçek <see cref="ITenantContext"/> kullanılır. Burada tenant çözülmeye gerek
/// olmadığından no-op bir bağlam verilir. Finance servisindeki desenle birebir.
/// </summary>
public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool HasTenant => false;
    }

    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=halos_inventory;Username=halos;Password=halos",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "inventory"))
            .Options;

        return new InventoryDbContext(options, new DesignTimeTenantContext());
    }
}

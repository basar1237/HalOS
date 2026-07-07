using HalOS.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HalOS.Sales.Infrastructure.Persistence;

/// <summary>
/// Tasarım-zamanı (dotnet ef) DbContext üreticisi. Migration üretimi için gereklidir; çalışma
/// zamanında DI'daki gerçek <see cref="ITenantContext"/> kullanılır. Burada tenant çözülmeye
/// gerek olmadığından no-op bir bağlam verilir. Party servisindeki desenle birebir.
/// </summary>
public sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool HasTenant => false;
    }

    public SalesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=halos_sales;Username=halos;Password=halos",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "sales"))
            .Options;

        return new SalesDbContext(options, new DesignTimeTenantContext());
    }
}

using HalOS.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HalOS.ColdChain.Infrastructure.Persistence;

/// <summary>
/// Tasarım-zamanı (dotnet ef) DbContext üreticisi. Migration üretimi için gereklidir; çalışma
/// zamanında DI'daki gerçek <see cref="ITenantContext"/> kullanılır. Inventory deseniyle birebir.
/// </summary>
public sealed class ColdChainDbContextFactory : IDesignTimeDbContextFactory<ColdChainDbContext>
{
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool HasTenant => false;
    }

    public ColdChainDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ColdChainDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=halos_coldchain;Username=halos;Password=halos",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "coldchain"))
            .Options;

        return new ColdChainDbContext(options, new DesignTimeTenantContext());
    }
}

using HalOS.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HalOS.Integration.Infrastructure.Persistence;

/// <summary>
/// Tasarım-zamanı (dotnet ef) DbContext üreticisi. Migration üretimi için gereklidir; çalışma
/// zamanında DI'daki gerçek <see cref="ITenantContext"/> kullanılır. Burada tenant çözülmeye
/// gerek olmadığından no-op bir bağlam verilir. Finance/Sales/Party servisindeki desenle birebir.
/// </summary>
public sealed class IntegrationDbContextFactory : IDesignTimeDbContextFactory<IntegrationDbContext>
{
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool HasTenant => false;
    }

    public IntegrationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=halos_integration;Username=halos;Password=halos",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "integration"))
            .Options;

        return new IntegrationDbContext(options, new DesignTimeTenantContext());
    }
}

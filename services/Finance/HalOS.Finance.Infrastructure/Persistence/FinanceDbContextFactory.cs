using HalOS.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HalOS.Finance.Infrastructure.Persistence;

/// <summary>
/// Tasarım-zamanı (dotnet ef) DbContext üreticisi. Migration üretimi için gereklidir; çalışma
/// zamanında DI'daki gerçek <see cref="ITenantContext"/> kullanılır. Burada tenant çözülmeye
/// gerek olmadığından no-op bir bağlam verilir. Sales/Party servisindeki desenle birebir.
/// </summary>
public sealed class FinanceDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool HasTenant => false;
    }

    public FinanceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=halos_finance;Username=halos;Password=halos",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "finance"))
            .Options;

        return new FinanceDbContext(options, new DesignTimeTenantContext());
    }
}

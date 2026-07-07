using HalOS.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HalOS.Identity.Infrastructure.Persistence;

/// <summary>
/// Tasarım-zamanı (dotnet ef) DbContext üreticisi. Migration üretimi için gereklidir;
/// çalışma zamanında DI'daki gerçek <see cref="ITenantContext"/> kullanılır. Burada
/// tenant çözümlenmeye gerek olmadığından no-op bir bağlam verilir.
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool HasTenant => false;
    }

    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=halos_identity;Username=halos;Password=halos",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
            .Options;

        return new IdentityDbContext(options, new DesignTimeTenantContext());
    }
}

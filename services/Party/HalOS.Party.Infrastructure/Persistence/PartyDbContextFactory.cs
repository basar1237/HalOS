using HalOS.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HalOS.Party.Infrastructure.Persistence;

/// <summary>
/// Tasarım-zamanı (dotnet ef) DbContext üreticisi. Migration üretimi için gereklidir;
/// çalışma zamanında DI'daki gerçek <see cref="ITenantContext"/> kullanılır. Burada
/// tenant çözümlenmeye gerek olmadığından no-op bir bağlam verilir.
/// </summary>
public sealed class PartyDbContextFactory : IDesignTimeDbContextFactory<PartyDbContext>
{
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool HasTenant => false;
    }

    public PartyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PartyDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=halos_party;Username=halos;Password=halos",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "party"))
            .Options;

        return new PartyDbContext(options, new DesignTimeTenantContext());
    }
}

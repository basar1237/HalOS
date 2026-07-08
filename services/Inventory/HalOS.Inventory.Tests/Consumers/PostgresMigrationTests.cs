using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Inventory.Tests.Consumers;

/// <summary>
/// Gerçek Postgres'e karşı migration uygulanabilirliğini doğrular (stock_item / stock_movement /
/// outbox_message tabloları + indeksler dahil). HALOS_TEST_POSTGRES ortam değişkeni yoksa SKIP edilir
/// (docs/07 §7). Bağlantı dizesi HALOS_TEST_POSTGRES_CONN ile geçersiz kılınabilir. Finance/Party
/// deseniyle birebir.
/// </summary>
public sealed class PostgresMigrationTests
{
    private sealed class NoTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool HasTenant => false;
    }

    [RequiresPostgresFact]
    public async Task Migrate_CreatesSchema()
    {
        var conn = Environment.GetEnvironmentVariable("HALOS_TEST_POSTGRES_CONN")
                   ?? "Host=localhost;Port=5432;Database=halos_inventory_test;Username=halos;Password=halos";

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(conn, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "inventory"))
            .Options;

        await using var ctx = new InventoryDbContext(options, new NoTenantContext());

        await ctx.Database.MigrateAsync();

        var canConnect = await ctx.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }
}

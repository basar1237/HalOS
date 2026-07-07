using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Party.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Party.Tests.Infrastructure;

/// <summary>
/// Gerçek Postgres'e karşı migration uygulanabilirliğini doğrular (partial unique index'ler
/// dahil). HALOS_TEST_POSTGRES ortam değişkeni yoksa SKIP edilir (docs/07 §7). Bağlantı dizesi
/// HALOS_TEST_POSTGRES_CONN ile geçersiz kılınabilir.
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
                   ?? "Host=localhost;Port=5432;Database=halos_party_test;Username=halos;Password=halos";

        var options = new DbContextOptionsBuilder<PartyDbContext>()
            .UseNpgsql(conn, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "party"))
            .Options;

        await using var ctx = new PartyDbContext(options, new NoTenantContext());

        await ctx.Database.MigrateAsync();

        var canConnect = await ctx.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }
}

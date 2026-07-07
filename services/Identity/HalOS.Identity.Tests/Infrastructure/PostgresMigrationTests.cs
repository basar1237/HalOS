using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Identity.Tests.Infrastructure;

/// <summary>
/// Gerçek Postgres'e karşı migration uygulanabilirliğini doğrular. HALOS_TEST_POSTGRES
/// ortam değişkeni yoksa SKIP edilir (docs/07 §7). Bağlantı dizesi HALOS_TEST_POSTGRES_CONN
/// ile geçersiz kılınabilir.
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
                   ?? "Host=localhost;Port=5432;Database=halos_identity_test;Username=halos;Password=halos";

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(conn, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
            .Options;

        await using var ctx = new IdentityDbContext(options, new NoTenantContext());

        await ctx.Database.MigrateAsync();

        var canConnect = await ctx.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }
}

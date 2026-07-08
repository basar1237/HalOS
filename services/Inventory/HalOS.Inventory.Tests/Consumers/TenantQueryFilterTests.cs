using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Domain.Aggregates;
using HalOS.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Inventory.Tests.Consumers;

/// <summary>
/// Tenant global query filter'ının çapraz-tenant erişimi engellediğini doğrular (docs/07 §6 / BK-8
/// zorunlu negatif test). EF Core InMemory sağlayıcısı global query filter'ı destekler. Finance
/// TenantQueryFilterTests deseniyle birebir.
/// </summary>
public sealed class TenantQueryFilterTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static InventoryDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new InventoryDbContext(options, tenantContext);
    }

    private static StockItem ItemWithIntake(Guid tenantId)
    {
        var item = StockItem.Open(tenantId, Guid.NewGuid(), Guid.NewGuid()).Value;
        item.RecordIntake(Guid.NewGuid(), 100.000m, new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc));
        return item;
    }

    [Fact]
    public async Task GlobalFilter_HidesOtherTenantsStockItems()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stubA = new StubTenantContext { TenantId = tenantA };

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.StockItems.Add(ItemWithIntake(tenantA));
            seed.StockItems.Add(ItemWithIntake(tenantB));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stubA, dbName))
        {
            var items = await ctx.StockItems.ToListAsync();
            items.Should().ContainSingle();
            items[0].TenantId.Should().Be(tenantA);
        }

        var stubB = new StubTenantContext { TenantId = tenantB };
        await using (var ctx = CreateContext(stubB, dbName))
        {
            var items = await ctx.StockItems.ToListAsync();
            items.Should().ContainSingle();
            items[0].TenantId.Should().Be(tenantB);
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_SeesAllTenants()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantA };

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.StockItems.Add(ItemWithIntake(tenantA));
            seed.StockItems.Add(ItemWithIntake(tenantB));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var all = await ctx.StockItems.IgnoreQueryFilters().ToListAsync();
            all.Should().HaveCount(2);
        }
    }
}

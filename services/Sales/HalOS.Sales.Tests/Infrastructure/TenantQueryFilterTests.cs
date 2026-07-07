using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;
using HalOS.Sales.Domain.ValueObjects;
using HalOS.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Sales.Tests.Infrastructure;

/// <summary>
/// Tenant global query filter'ının çapraz-tenant erişimi engellediğini doğrular (docs/07 §6 /
/// BK-8 zorunlu negatif test) ve tamamlanan satışın SaleCompleted event'ini outbox'a tenant'lı
/// yazdığını doğrular (docs/04 §10). EF Core InMemory sağlayıcısı global query filter'ı destekler.
/// </summary>
public sealed class TenantQueryFilterTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static SalesDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SalesDbContext(options, tenantContext);
    }

    private static SaleTransaction NewCompletedSale(Guid tenantId)
    {
        var sale = SaleTransaction.Create(
            tenantId, Guid.NewGuid(), Guid.NewGuid(), null,
            new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc),
            isWithinMarket: true, operationId: Guid.NewGuid(), createdBy: Guid.NewGuid()).Value;
        sale.AddLine(Guid.NewGuid(), 100m, UnitOfMeasure.Kilogram, 1m);
        sale.Complete(RateSet.Create(0.08m, 0.02m, 0.01m, true, 0.20m).Value);
        return sale;
    }

    [Fact]
    public async Task GlobalFilter_HidesOtherTenantsSales()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var stubA = new StubTenantContext { TenantId = tenantA };

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.SaleTransactions.Add(NewCompletedSale(tenantA));
            seed.SaleTransactions.Add(NewCompletedSale(tenantB));
            await seed.SaveChangesAsync();
        }

        // Tenant A bağlamında yalnızca A'nın satışı görünmeli.
        await using (var ctx = CreateContext(stubA, dbName))
        {
            var sales = await ctx.SaleTransactions.ToListAsync();
            sales.Should().ContainSingle();
            sales[0].TenantId.Should().Be(tenantA);
        }

        // Tenant B bağlamında yalnızca B'nin satışı görünmeli.
        var stubB = new StubTenantContext { TenantId = tenantB };
        await using (var ctx = CreateContext(stubB, dbName))
        {
            var sales = await ctx.SaleTransactions.ToListAsync();
            sales.Should().ContainSingle();
            sales[0].TenantId.Should().Be(tenantB);
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
            seed.SaleTransactions.Add(NewCompletedSale(tenantA));
            seed.SaleTransactions.Add(NewCompletedSale(tenantB));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var all = await ctx.SaleTransactions.IgnoreQueryFilters().ToListAsync();
            all.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task SaveChanges_WritesSaleCompletedToOutbox_WithTenant()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        ctx.SaleTransactions.Add(NewCompletedSale(tenantId));
        await ctx.SaveChangesAsync();

        var outbox = await ctx.OutboxMessages.ToListAsync();
        outbox.Should().ContainSingle(m => m.Type.Contains("SaleCompleted"));
        outbox.Single(m => m.Type.Contains("SaleCompleted")).TenantId.Should().Be(tenantId);
    }
}

using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Domain.Aggregates;
using HalOS.Finance.Domain.Enums;
using HalOS.Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Finance.Tests.Consumers;

/// <summary>
/// Tenant global query filter'ının çapraz-tenant erişimi engellediğini doğrular (docs/07 §6 /
/// BK-8 zorunlu negatif test). EF Core InMemory sağlayıcısı global query filter'ı destekler.
/// </summary>
public sealed class TenantQueryFilterTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static FinanceDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new FinanceDbContext(options, tenantContext);
    }

    private static CurrentAccount AccountWithSale(Guid tenantId)
    {
        var account = CurrentAccount.Open(tenantId, Guid.NewGuid()).Value;
        account.RecordSaleDebit(Guid.NewGuid(), 100.00m, new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc));
        return account;
    }

    [Fact]
    public async Task GlobalFilter_HidesOtherTenantsAccounts()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stubA = new StubTenantContext { TenantId = tenantA };

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.CurrentAccounts.Add(AccountWithSale(tenantA));
            seed.CurrentAccounts.Add(AccountWithSale(tenantB));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stubA, dbName))
        {
            var accounts = await ctx.CurrentAccounts.ToListAsync();
            accounts.Should().ContainSingle();
            accounts[0].TenantId.Should().Be(tenantA);
        }

        var stubB = new StubTenantContext { TenantId = tenantB };
        await using (var ctx = CreateContext(stubB, dbName))
        {
            var accounts = await ctx.CurrentAccounts.ToListAsync();
            accounts.Should().ContainSingle();
            accounts[0].TenantId.Should().Be(tenantB);
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
            seed.CurrentAccounts.Add(AccountWithSale(tenantA));
            seed.CurrentAccounts.Add(AccountWithSale(tenantB));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var all = await ctx.CurrentAccounts.IgnoreQueryFilters().ToListAsync();
            all.Should().HaveCount(2);
        }
    }
}

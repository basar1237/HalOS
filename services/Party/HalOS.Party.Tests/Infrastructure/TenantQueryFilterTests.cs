using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Party.Domain.Enums;
using HalOS.Party.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PartyAggregate = HalOS.Party.Domain.Aggregates.Party;

namespace HalOS.Party.Tests.Infrastructure;

/// <summary>
/// Tenant global query filter'ının çapraz-tenant erişimi engellediğini doğrular (docs/07 §6 /
/// BK-8 zorunlu negatif test). EF Core InMemory sağlayıcısı global query filter'ı destekler.
/// Ayrıca domain event'lerin outbox'a tenant'lı yazıldığını doğrular (docs/04 §10).
/// </summary>
public sealed class TenantQueryFilterTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static PartyDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<PartyDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new PartyDbContext(options, tenantContext);
    }

    private static PartyAggregate NewParty(Guid tenantId, string name) =>
        PartyAggregate.Register(
            tenantId, name, null, null, null, null, null,
            keepsRecords: true, withholdingProfile: null,
            roles: new[] { PartyRoleType.Buyer }).Value;

    [Fact]
    public async Task GlobalFilter_HidesOtherTenantsParties()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var stub = new StubTenantContext { TenantId = tenantA };

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.Parties.Add(NewParty(tenantA, "A Manavi"));
            seed.Parties.Add(NewParty(tenantB, "B Manavi"));
            await seed.SaveChangesAsync();
        }

        // Tenant A bağlamında yalnızca A'nın tarafı görünmeli.
        await using (var ctx = CreateContext(stub, dbName))
        {
            var parties = await ctx.Parties.ToListAsync();
            parties.Should().ContainSingle();
            parties[0].TenantId.Should().Be(tenantA);
        }

        // Tenant B bağlamında yalnızca B'nin tarafı görünmeli.
        var stubB = new StubTenantContext { TenantId = tenantB };
        await using (var ctx = CreateContext(stubB, dbName))
        {
            var parties = await ctx.Parties.ToListAsync();
            parties.Should().ContainSingle();
            parties[0].TenantId.Should().Be(tenantB);
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
            seed.Parties.Add(NewParty(tenantA, "A"));
            seed.Parties.Add(NewParty(tenantB, "B"));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var all = await ctx.Parties.IgnoreQueryFilters().ToListAsync();
            all.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task SaveChanges_WritesPartyRegisteredToOutbox()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        ctx.Parties.Add(NewParty(tenantId, "Outbox Manavi"));
        await ctx.SaveChangesAsync();

        var outbox = await ctx.OutboxMessages.ToListAsync();
        outbox.Should().ContainSingle(m => m.Type.Contains("PartyRegistered"));
        outbox[0].TenantId.Should().Be(tenantId);
    }
}

using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Integration.Domain.ReadModels;
using HalOS.Integration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Integration.Tests.Consumers;

/// <summary>
/// Tenant global query filter'ının çapraz-tenant erişimi engellediğini doğrular (docs/07 §6 / BK-8
/// zorunlu negatif test). ProducerTaxProfile okuma modeli üzerinden. Finance/Sales deseniyle birebir.
/// </summary>
public sealed class TenantQueryFilterTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static IntegrationDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new IntegrationDbContext(options, tenantContext);
    }

    private static ProducerTaxProfile Profile(Guid tenantId) =>
        ProducerTaxProfile.Create(tenantId, Guid.NewGuid(), keepsRecords: false, 0.02m, 0.01m,
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task GlobalFilter_HidesOtherTenantsProfiles()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stubA = new StubTenantContext { TenantId = tenantA };

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.ProducerTaxProfiles.Add(Profile(tenantA));
            seed.ProducerTaxProfiles.Add(Profile(tenantB));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stubA, dbName))
        {
            var profiles = await ctx.ProducerTaxProfiles.ToListAsync();
            profiles.Should().ContainSingle();
            profiles[0].TenantId.Should().Be(tenantA);
        }

        var stubB = new StubTenantContext { TenantId = tenantB };
        await using (var ctx = CreateContext(stubB, dbName))
        {
            var profiles = await ctx.ProducerTaxProfiles.ToListAsync();
            profiles.Should().ContainSingle();
            profiles[0].TenantId.Should().Be(tenantB);
        }
    }
}

using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.Enums;
using HalOS.Identity.Domain.ValueObjects;
using HalOS.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Identity.Tests.Infrastructure;

/// <summary>
/// Tenant global query filter'ının çapraz-tenant erişimi engellediğini doğrular (docs/07 §6
/// zorunlu negatif test). EF Core InMemory sağlayıcısı global query filter'ı destekler.
/// </summary>
public sealed class TenantQueryFilterTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static IdentityDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(dbName)
            // Tenant izolasyonu dinamik closure filtresiyle sağlanır: filtre her sorguda
            // ITenantContext.TenantId'yi canlı okur, tek model tüm tenant'lar için doğrudur.
            // Model önbellek anahtarını çeşitlendirmeye gerek yok (BK-8).
            .Options;
        return new IdentityDbContext(options, tenantContext);
    }

    private static User NewUser(Guid tenantId, string email)
    {
        return User.Register(
            tenantId,
            Email.Create(email).Value,
            PasswordHash.Create("hash").Value,
            "Kullanici",
            SystemRole.Cashier).Value;
    }

    [Fact]
    public async Task GlobalFilter_HidesOtherTenantsUsers()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var stub = new StubTenantContext { TenantId = tenantA };

        // Aynı isim InMemory DB'yi paylaşır; iki tenant'ın kullanıcısını seed et.
        await using (var seed = CreateContext(stub, dbName))
        {
            seed.Users.Add(NewUser(tenantA, "a@hal.com"));
            seed.Users.Add(NewUser(tenantB, "b@hal.com"));
            await seed.SaveChangesAsync();
        }

        // Tenant A bağlamında yalnızca A'nın kullanıcısı görünmeli.
        await using (var ctx = CreateContext(stub, dbName))
        {
            var users = await ctx.Users.ToListAsync();
            users.Should().ContainSingle();
            users[0].TenantId.Should().Be(tenantA);
        }

        // Tenant B bağlamında yalnızca B'nin kullanıcısı görünmeli.
        var stubB = new StubTenantContext { TenantId = tenantB };
        await using (var ctx = CreateContext(stubB, dbName))
        {
            var users = await ctx.Users.ToListAsync();
            users.Should().ContainSingle();
            users[0].TenantId.Should().Be(tenantB);
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_AllowsCrossTenantForAuthFlows()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantA };

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.Users.Add(NewUser(tenantA, "a@hal.com"));
            seed.Users.Add(NewUser(tenantB, "b@hal.com"));
            await seed.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var all = await ctx.Users.IgnoreQueryFilters().ToListAsync();
            all.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task SaveChanges_WritesDomainEventsToOutbox()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        ctx.Users.Add(NewUser(tenantId, "e@hal.com"));
        await ctx.SaveChangesAsync();

        var outbox = await ctx.OutboxMessages.ToListAsync();
        outbox.Should().ContainSingle(m => m.Type.Contains("UserRegistered"));
        outbox[0].TenantId.Should().Be(tenantId);
    }
}

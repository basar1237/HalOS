using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Sales.Infrastructure.Messaging;
using HalOS.Sales.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HalOS.Sales.Tests.Infrastructure;

/// <summary>
/// ProducerWithholdingProfileChangedConsumer testleri (docs/02 §6). Party'den gelen oran değişimini
/// ProducerRateProfile okuma modeline UPSERT eder: ilk olayda oluşturur, tekrar gelen olayda
/// mevcut satırı günceller (yeni satır AÇMAZ). Tenant, testte doğrudan DbContext bağlamına
/// verilir (çalışma zamanında TenantConsumeFilter mesajdan doldurur — BK-8). EF Core InMemory.
/// </summary>
public sealed class ProducerWithholdingProfileChangedConsumerTests
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

    private static ConsumeContext<ProducerWithholdingProfileChanged> ContextFor(
        ProducerWithholdingProfileChanged message)
    {
        var mock = new Mock<ConsumeContext<ProducerWithholdingProfileChanged>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    [Fact]
    public async Task Consume_NewProducer_InsertsReadModel()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        var consumer = new ProducerWithholdingProfileChangedConsumer(
            ctx, NullLogger<ProducerWithholdingProfileChangedConsumer>.Instance);

        await consumer.Consume(ContextFor(new ProducerWithholdingProfileChanged(
            tenantId, producerId, AgriWithholdingRate: 0.02m, FarmerSskRate: 0.01m, DateTime.UtcNow)));

        var profiles = await ctx.ProducerRateProfiles.ToListAsync();
        profiles.Should().ContainSingle();
        profiles[0].TenantId.Should().Be(tenantId);
        profiles[0].ProducerPartyId.Should().Be(producerId);
        profiles[0].AgriWithholdingRate.Should().Be(0.02m);
        profiles[0].FarmerSskRate.Should().Be(0.01m);
    }

    [Fact]
    public async Task Consume_ExistingProducer_UpdatesInPlace_NoDuplicate()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        // İlk olay: oluştur.
        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new ProducerWithholdingProfileChangedConsumer(
                ctx, NullLogger<ProducerWithholdingProfileChangedConsumer>.Instance);
            await consumer.Consume(ContextFor(new ProducerWithholdingProfileChanged(
                tenantId, producerId, 0.02m, 0.01m, DateTime.UtcNow)));
        }

        // İkinci olay (yeni oranlar): mevcut satırı güncelle, yeni satır açma.
        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new ProducerWithholdingProfileChangedConsumer(
                ctx, NullLogger<ProducerWithholdingProfileChangedConsumer>.Instance);
            await consumer.Consume(ContextFor(new ProducerWithholdingProfileChanged(
                tenantId, producerId, AgriWithholdingRate: 0.05m, FarmerSskRate: 0.03m, DateTime.UtcNow)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var profiles = await ctx.ProducerRateProfiles.ToListAsync();
            profiles.Should().ContainSingle();
            profiles[0].AgriWithholdingRate.Should().Be(0.05m);
            profiles[0].FarmerSskRate.Should().Be(0.03m);
        }
    }

    [Fact]
    public async Task Consume_OutOfOrderStaleEvent_DoesNotOverwriteNewerRates()
    {
        // RabbitMQ sıra garantisi vermez (docs/04 §10): daha ESKİ zaman damgalı bir event, daha
        // YENİ event'ten sonra gelirse güncel oranları bayat değerlerle geri almamalı. Apply monoton
        // olduğundan sıra-dışı eski event yok sayılır (bayat oran → yanlış net hakediş, BK-1).
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        var newer = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);
        var older = new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);

        // Önce YENİ event işlenir (güncel oranlar).
        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new ProducerWithholdingProfileChangedConsumer(
                ctx, NullLogger<ProducerWithholdingProfileChangedConsumer>.Instance);
            await consumer.Consume(ContextFor(new ProducerWithholdingProfileChanged(
                tenantId, producerId, AgriWithholdingRate: 0.05m, FarmerSskRate: 0.03m, newer)));
        }

        // Sonra ESKİ event gelir (sıra-dışı teslimat) — yok sayılmalı.
        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new ProducerWithholdingProfileChangedConsumer(
                ctx, NullLogger<ProducerWithholdingProfileChangedConsumer>.Instance);
            await consumer.Consume(ContextFor(new ProducerWithholdingProfileChanged(
                tenantId, producerId, AgriWithholdingRate: 0.02m, FarmerSskRate: 0.01m, older)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var profiles = await ctx.ProducerRateProfiles.ToListAsync();
            profiles.Should().ContainSingle();
            profiles[0].AgriWithholdingRate.Should().Be(0.05m); // YENİ oran korundu.
            profiles[0].FarmerSskRate.Should().Be(0.03m);
            profiles[0].UpdatedAtUtc.Should().Be(newer);        // zaman damgası geri alınmadı.
        }
    }
}

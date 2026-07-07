using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Integration.Application.Consumers;
using HalOS.Integration.Infrastructure.Persistence;
using HalOS.Integration.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HalOS.Integration.Tests.Consumers;

/// <summary>
/// ProducerWithholdingProfileChangedConsumer testleri (docs/02 §6; Party → Integration senkronu).
/// Gerçek IntegrationDbContext (InMemory) + gerçek ProducerTaxProfileWriter/Reader ile upsert ve
/// sıra-dışı (out-of-order) monoton guard davranışını doğrular.
/// </summary>
public sealed class ProducerWithholdingProfileChangedConsumerTests
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

    private static ConsumeContext<ProducerWithholdingProfileChanged> ContextFor(ProducerWithholdingProfileChanged message)
    {
        var mock = new Mock<ConsumeContext<ProducerWithholdingProfileChanged>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    [Fact]
    public async Task Consume_UpsertsProfile_ThenUpdatesOnNewerEvent()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        var consumer = new ProducerWithholdingProfileChangedConsumer(
            new ProducerTaxProfileWriter(ctx), ctx, NullLogger<ProducerWithholdingProfileChangedConsumer>.Instance);

        await consumer.Consume(ContextFor(new ProducerWithholdingProfileChanged(
            tenantId, producerId, KeepsRecords: false, AgriWithholdingRate: 0.02m, FarmerSskRate: 0.01m,
            OccurredOnUtc: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc))));

        // Daha yeni event: KeepsRecords true'ya döner.
        await consumer.Consume(ContextFor(new ProducerWithholdingProfileChanged(
            tenantId, producerId, KeepsRecords: true, AgriWithholdingRate: 0.02m, FarmerSskRate: 0.01m,
            OccurredOnUtc: new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc))));

        var profile = await ctx.ProducerTaxProfiles.IgnoreQueryFilters().SingleAsync();
        profile.KeepsRecords.Should().BeTrue();
        profile.UpdatedAtUtc.Should().Be(new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Consume_OutOfOrderStaleEvent_DoesNotOverwriteNewerProfile()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        var consumer = new ProducerWithholdingProfileChangedConsumer(
            new ProducerTaxProfileWriter(ctx), ctx, NullLogger<ProducerWithholdingProfileChangedConsumer>.Instance);

        // Önce YENİ event (KeepsRecords=true, 07-05).
        await consumer.Consume(ContextFor(new ProducerWithholdingProfileChanged(
            tenantId, producerId, KeepsRecords: true, AgriWithholdingRate: 0.02m, FarmerSskRate: 0.01m,
            OccurredOnUtc: new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc))));

        // Sonra ESKİ event (KeepsRecords=false, 07-01) — sıra-dışı; UYGULANMAMALI (monoton guard).
        await consumer.Consume(ContextFor(new ProducerWithholdingProfileChanged(
            tenantId, producerId, KeepsRecords: false, AgriWithholdingRate: 0.09m, FarmerSskRate: 0.09m,
            OccurredOnUtc: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc))));

        var profile = await ctx.ProducerTaxProfiles.IgnoreQueryFilters().SingleAsync();
        profile.KeepsRecords.Should().BeTrue(); // yeni değer korunur; bayat event yok sayıldı
        profile.AgriWithholdingRate.Should().Be(0.02m);
        profile.UpdatedAtUtc.Should().Be(new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc));
    }
}

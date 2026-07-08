using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Contracts;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Consumers;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;
using HalOS.Integration.Infrastructure.Persistence;
using HalOS.Integration.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HalOS.Integration.Tests.Consumers;

/// <summary>
/// ConsignmentReceivedConsumer testleri (docs/02 §6 satır 229: ConsignmentReceived → künye; docs/03
/// M8 / BK-4). Gerçek IntegrationDbContext (InMemory) + gerçek ProductPassportRepository ile:
/// <list type="bullet">
///   <item>Mal geliş partisinin HER kalemi için künye üretilir (19-haneli HKS kod, Issued).</item>
///   <item>Idempotency: aynı event iki kez → çift künye oluşmaz (kalem başına tek).</item>
/// </list>
/// </summary>
public sealed class ConsignmentReceivedConsumerTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private sealed class FakeGateway : IEDocumentGateway
    {
        public int PassportCount { get; private set; }

        public Task<Result<string>> SendProducerReceiptAsync(ProducerReceipt receipt, CancellationToken cancellationToken = default)
            => Task.FromResult<Result<string>>("EMM-TEST");

        public Task<Result<string>> SendInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
            => Task.FromResult<Result<string>>("EFA-TEST");

        public Task<Result<string>> SendHksNotificationAsync(HksNotification notification, CancellationToken cancellationToken = default)
            => Task.FromResult<Result<string>>("HKS-TEST");

        public Task<Result<string>> GenerateProductPassportAsync(ProductPassport passport, CancellationToken cancellationToken = default)
        {
            PassportCount++;
            // 19 haneli sahte kod (her çağrıda benzersiz son hane).
            return Task.FromResult<Result<string>>($"123456789012345678{PassportCount % 10}");
        }
    }

    private static IntegrationDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new IntegrationDbContext(options, tenantContext);
    }

    private static ConsumeContext<ConsignmentReceived> ContextFor(ConsignmentReceived message)
    {
        var mock = new Mock<ConsumeContext<ConsignmentReceived>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    private static ConsignmentReceived SampleConsignment(Guid tenantId, Guid consignmentId, params Guid[] itemIds)
    {
        var items = itemIds
            .Select(id => new ConsignmentReceivedItem(id, Guid.NewGuid(), 50.000m, "Kilogram"))
            .ToList();
        return new ConsignmentReceived(
            consignmentId, tenantId, Guid.NewGuid(), new DateTime(2026, 7, 6, 8, 0, 0, DateTimeKind.Utc),
            items, DateTime.UtcNow);
    }

    [Fact]
    public async Task Consume_ProducesOnePassportPerItem_WithNineteenDigitCode()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        var gateway = new FakeGateway();
        var consumer = new ConsignmentReceivedConsumer(
            new ProductPassportRepository(ctx), gateway, ctx, NullLogger<ConsignmentReceivedConsumer>.Instance);

        var consignmentId = Guid.NewGuid();
        await consumer.Consume(ContextFor(SampleConsignment(tenantId, consignmentId, Guid.NewGuid(), Guid.NewGuid())));

        var passports = await ctx.ProductPassports.IgnoreQueryFilters().ToListAsync();
        passports.Should().HaveCount(2);
        passports.Should().OnlyContain(p => p.Status == ProductPassportStatus.Issued);
        passports.Should().OnlyContain(p => p.PassportCode!.Length == 19);
        passports.Should().OnlyContain(p => p.ConsignmentId == consignmentId);
        gateway.PassportCount.Should().Be(2);
    }

    [Fact]
    public async Task Consume_SameEventTwice_IsIdempotent_NoDuplicatePassports()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        var gateway = new FakeGateway();
        var consumer = new ConsignmentReceivedConsumer(
            new ProductPassportRepository(ctx), gateway, ctx, NullLogger<ConsignmentReceivedConsumer>.Instance);

        var msg = SampleConsignment(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await consumer.Consume(ContextFor(msg));
        await consumer.Consume(ContextFor(msg));

        (await ctx.ProductPassports.IgnoreQueryFilters().CountAsync()).Should().Be(2);
        gateway.PassportCount.Should().Be(2); // ikinci tüketimde yeni kod üretilmez (idempotent)
    }
}

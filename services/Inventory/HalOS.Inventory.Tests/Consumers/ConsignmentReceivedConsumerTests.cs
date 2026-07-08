using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Inventory.Application.Consumers;
using HalOS.Inventory.Domain.Enums;
using HalOS.Inventory.Infrastructure.Persistence;
using HalOS.Inventory.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HalOS.Inventory.Tests.Consumers;

/// <summary>
/// ConsignmentReceivedConsumer testleri (docs/02 §229: ConsignmentReceived → stok girişi). Gerçek
/// InventoryDbContext (EF Core InMemory) + gerçek StockItemRepository ile uçtan uca doğrular:
/// <list type="bullet">
///   <item>Mal geliş partisinin HER kalemi için ilgili ürünün stoğu artar (giriş).</item>
///   <item>Idempotency: aynı event iki kez → çift giriş oluşmaz (kalem başına tek).</item>
///   <item>Stok kalemi yoksa açılır (upsert); tenant testte DbContext bağlamına verilir.</item>
/// </list>
/// </summary>
public sealed class ConsignmentReceivedConsumerTests
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

    private static ConsumeContext<ConsignmentReceived> ContextFor(ConsignmentReceived message)
    {
        var mock = new Mock<ConsumeContext<ConsignmentReceived>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    private static ConsignmentReceived SampleConsignment(
        Guid tenantId, Guid consignmentId, params (Guid ItemId, Guid ProductId, decimal Qty)[] items)
    {
        var lines = items
            .Select(x => new ConsignmentReceivedItem(x.ItemId, x.ProductId, x.Qty, "Kilogram"))
            .ToList();
        return new ConsignmentReceived(
            consignmentId, tenantId, Guid.NewGuid(), new DateTime(2026, 7, 6, 8, 0, 0, DateTimeKind.Utc),
            lines, DateTime.UtcNow);
    }

    [Fact]
    public async Task Consume_RecordsIntakePerItem_QuantityIncreases()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new ConsignmentReceivedConsumer(
                new StockItemRepository(ctx), ctx, NullLogger<ConsignmentReceivedConsumer>.Instance);
            await consumer.Consume(ContextFor(SampleConsignment(
                tenantId, Guid.NewGuid(),
                (Guid.NewGuid(), productA, 100.000m),
                (Guid.NewGuid(), productB, 50.000m))));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var a = await ctx.StockItems.Include(i => i.Movements).FirstAsync(i => i.ProductId == productA);
            var b = await ctx.StockItems.Include(i => i.Movements).FirstAsync(i => i.ProductId == productB);

            a.QuantityOnHand.Should().Be(100.000m);
            a.Movements.Single().Kind.Should().Be(StockMovementKind.Intake);
            b.QuantityOnHand.Should().Be(50.000m);
        }
    }

    [Fact]
    public async Task Consume_SameEventTwice_IsIdempotent_NoDuplicateIntake()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productA = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var message = SampleConsignment(tenantId, Guid.NewGuid(), (Guid.NewGuid(), productA, 100.000m));

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new ConsignmentReceivedConsumer(
                new StockItemRepository(ctx), ctx, NullLogger<ConsignmentReceivedConsumer>.Instance);
            await consumer.Consume(ContextFor(message));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new ConsignmentReceivedConsumer(
                new StockItemRepository(ctx), ctx, NullLogger<ConsignmentReceivedConsumer>.Instance);
            await consumer.Consume(ContextFor(message)); // broker retry
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var a = await ctx.StockItems.Include(i => i.Movements).FirstAsync(i => i.ProductId == productA);
            a.Movements.Should().ContainSingle(); // çift giriş YOK
            a.QuantityOnHand.Should().Be(100.000m);
        }
    }
}

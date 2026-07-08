using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Inventory.Application.Consumers;
using HalOS.Inventory.Domain.Aggregates;
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
/// SaleCompletedConsumer testleri (docs/02 §230: SaleCompleted → stok çıkışı). Gerçek
/// InventoryDbContext (InMemory) + gerçek StockItemRepository ile:
/// <list type="bullet">
///   <item>Satışın HER satırı için ilgili ürünün stoğu azalır (çıkış); kalan = giriş − çıkış.</item>
///   <item>BK-7: çıkış mevcut stoğu aşarsa consumer istisna fırlatır ve HİÇBİR şey kalıcılaşmaz.</item>
///   <item>Idempotency: aynı satış iki kez tüketilse çift çıkış oluşmaz (docs/04 §5).</item>
/// </list>
/// </summary>
public sealed class SaleCompletedConsumerTests
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

    private static SaleCompletedConsumer NewConsumer(InventoryDbContext ctx)
    {
        var stockItems = new StockItemRepository(ctx);
        var warehouses = new WarehouseRepository(ctx);
        var provider = new WarehouseProvider(warehouses);
        return new SaleCompletedConsumer(
            stockItems, provider, ctx, NullLogger<SaleCompletedConsumer>.Instance);
    }

    private static ConsumeContext<SaleCompleted> ContextFor(SaleCompleted message)
    {
        var mock = new Mock<ConsumeContext<SaleCompleted>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    private static SaleCompleted SampleSale(Guid tenantId, Guid saleId, params (Guid LineId, Guid ProductId, decimal Qty)[] lines)
    {
        var saleLines = lines
            .Select(x => new SaleCompletedLine(x.LineId, x.ProductId, x.Qty, "Kilogram"))
            .ToList();
        return new SaleCompleted(
            SaleTransactionId: saleId,
            TenantId: tenantId,
            BuyerPartyId: Guid.NewGuid(),
            ProducerPartyId: Guid.NewGuid(),
            SoldAt: new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc),
            GrossAmount: 100.00m,
            CommissionAmount: 8.00m,
            CommissionVatAmount: 1.60m,
            AgriWithholdingAmount: 2.00m,
            FarmerSskAmount: 1.00m,
            MarketFeeAmount: 1.00m,
            TotalDeductions: 12.00m,
            NetAmount: 88.00m,
            SettlementDueDate: new DateTime(2026, 7, 28),
            Lines: saleLines,
            OccurredOnUtc: DateTime.UtcNow);
    }

    /// <summary>
    /// Varsayılan depo (MERKEZ) + belirli bir ürün için önceden stok girişi olan bir kalem seed'ler.
    /// Consumer varsayılan depoya çıkış yazacağından kalem de o depoya açılır (docs/06 S2.1).
    /// </summary>
    private static async Task SeedStockAsync(InventoryDbContext ctx, Guid tenantId, Guid productId, decimal quantity)
    {
        var warehouse = Warehouse.Create(tenantId, "Merkez Depo", "MERKEZ", isDefault: true).Value;
        ctx.Warehouses.Add(warehouse);

        var item = StockItem.Open(tenantId, warehouse.Id, productId).Value;
        item.RecordIntake(Guid.NewGuid(), quantity, new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc));
        ctx.StockItems.Add(item);
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Consume_RecordsSaleOutPerLine_QuantityDecreases()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            await SeedStockAsync(ctx, tenantId, productId, 100.000m);
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = NewConsumer(ctx);
            await consumer.Consume(ContextFor(SampleSale(tenantId, Guid.NewGuid(), (Guid.NewGuid(), productId, 30.000m))));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var item = await ctx.StockItems.Include(i => i.Movements).FirstAsync(i => i.ProductId == productId);
            item.QuantityOnHand.Should().Be(70.000m); // 100 − 30
            item.Movements.Single(m => m.Kind == StockMovementKind.SaleOut).SignedQuantity.Should().Be(-30.000m);
        }
    }

    [Fact]
    public async Task Consume_ExceedingStock_Throws_AndPersistsNothing_BK7()
    {
        // BK-7: çıkış mevcut stoğu aşarsa (kalan negatif olacaksa) consumer istisna fırlatmalı ve
        // HİÇBİR şey kalıcılaşmamalı → MassTransit retry/error queue (docs/04 §10).
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            await SeedStockAsync(ctx, tenantId, productId, 10.000m);
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = NewConsumer(ctx);

            var act = () => consumer.Consume(ContextFor(SampleSale(tenantId, Guid.NewGuid(), (Guid.NewGuid(), productId, 11.000m))));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var item = await ctx.StockItems.Include(i => i.Movements).FirstAsync(i => i.ProductId == productId);
            // Yalnız seed girişi kalmalı; çıkış kalıcılaşmamış olmalı.
            item.Movements.Should().ContainSingle();
            item.QuantityOnHand.Should().Be(10.000m);
            item.Movements.Should().NotContain(m => m.Kind == StockMovementKind.SaleOut);
        }
    }

    [Fact]
    public async Task Consume_SameSaleTwice_IsIdempotent_NoDuplicateSaleOut()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var message = SampleSale(tenantId, Guid.NewGuid(), (Guid.NewGuid(), productId, 30.000m));

        await using (var ctx = CreateContext(stub, dbName))
        {
            await SeedStockAsync(ctx, tenantId, productId, 100.000m);
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = NewConsumer(ctx);
            await consumer.Consume(ContextFor(message));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = NewConsumer(ctx);
            await consumer.Consume(ContextFor(message)); // broker retry
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var item = await ctx.StockItems.Include(i => i.Movements).FirstAsync(i => i.ProductId == productId);
            item.Movements.Count(m => m.Kind == StockMovementKind.SaleOut).Should().Be(1); // çift çıkış YOK
            item.QuantityOnHand.Should().Be(70.000m);
        }
    }
}

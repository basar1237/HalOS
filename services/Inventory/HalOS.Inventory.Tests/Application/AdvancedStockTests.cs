using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Inventory.Application.Consumers;
using HalOS.Inventory.Application.Features.ListLowStock;
using HalOS.Inventory.Application.Features.Reports.SpoilageAnalysisReport;
using HalOS.Inventory.Application.Features.SetReorderThreshold;
using HalOS.Inventory.Domain.Aggregates;
using HalOS.Inventory.Infrastructure.Persistence;
using HalOS.Inventory.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HalOS.Inventory.Tests.Application;

/// <summary>
/// Gelişmiş stok testleri (docs/06 S2.1): varsayılan depo oluşturma, UNIQUE(tenant, depo, ürün),
/// yeniden-sipariş eşiği altına inince LowStockAlerted (outbox), düşük stok listesi ve fire analizi
/// oranı. Gerçek InventoryDbContext (InMemory) + gerçek repository'ler/provider ile uçtan uca.
/// </summary>
public sealed class AdvancedStockTests
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

    private static ConsumeContext<T> ContextFor<T>(T message)
        where T : class
    {
        var mock = new Mock<ConsumeContext<T>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    private static ConsignmentReceived Consignment(Guid tenantId, Guid productId, decimal qty) =>
        new(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            new List<ConsignmentReceivedItem> { new(Guid.NewGuid(), productId, qty, "Kilogram") },
            DateTime.UtcNow);

    private static SaleCompleted Sale(Guid tenantId, Guid productId, decimal qty) =>
        new(
            SaleTransactionId: Guid.NewGuid(),
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
            Lines: new List<SaleCompletedLine> { new(Guid.NewGuid(), productId, qty, "Kilogram") },
            OccurredOnUtc: DateTime.UtcNow);

    private static ConsignmentReceivedConsumer IntakeConsumer(InventoryDbContext ctx) =>
        new(new StockItemRepository(ctx), new WarehouseProvider(new WarehouseRepository(ctx)), ctx,
            NullLogger<ConsignmentReceivedConsumer>.Instance);

    private static SaleCompletedConsumer SaleConsumer(InventoryDbContext ctx) =>
        new(new StockItemRepository(ctx), new WarehouseProvider(new WarehouseRepository(ctx)), ctx,
            NullLogger<SaleCompletedConsumer>.Instance);

    [Fact]
    public async Task Intake_WhenNoDefaultWarehouse_CreatesMerkezDepo_AndWritesThere()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            await IntakeConsumer(ctx).Consume(ContextFor(Consignment(tenantId, productId, 100.000m)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var wh = await ctx.Warehouses.SingleAsync();
            wh.Code.Should().Be("MERKEZ");
            wh.IsDefault.Should().BeTrue();

            var item = await ctx.StockItems.Include(i => i.Movements).SingleAsync();
            item.WarehouseId.Should().Be(wh.Id);
            item.QuantityOnHand.Should().Be(100.000m);
        }
    }

    [Fact]
    public async Task UniqueConstraint_SameProductSameWarehouse_UsesSingleStockItem()
    {
        // İki ayrı mal geliş partisi aynı ürüne → tek stok kalemi (UNIQUE(tenant, depo, ürün)).
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            await IntakeConsumer(ctx).Consume(ContextFor(Consignment(tenantId, productId, 100.000m)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            await IntakeConsumer(ctx).Consume(ContextFor(Consignment(tenantId, productId, 40.000m)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var items = await ctx.StockItems.Include(i => i.Movements)
                .Where(i => i.ProductId == productId).ToListAsync();
            items.Should().ContainSingle(); // tek kalem
            items[0].QuantityOnHand.Should().Be(140.000m); // 100 + 40
        }
    }

    [Fact]
    public async Task Sale_DropsBelowReorderThreshold_RaisesLowStockAlerted_ToOutbox()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        // Giriş 100 + eşik 20 (varsayılan depoda kalem oluşur).
        await using (var ctx = CreateContext(stub, dbName))
        {
            await IntakeConsumer(ctx).Consume(ContextFor(Consignment(tenantId, productId, 100.000m)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new SetReorderThresholdHandler(
                new StockItemRepository(ctx), new WarehouseRepository(ctx), ctx);
            var set = await handler.Handle(new SetReorderThresholdCommand(productId, 20.000m), CancellationToken.None);
            set.IsSuccess.Should().BeTrue();
        }

        // Satış 85 → kalan 15 (< 20) → LowStockAlerted.
        await using (var ctx = CreateContext(stub, dbName))
        {
            await SaleConsumer(ctx).Consume(ContextFor(Sale(tenantId, productId, 85.000m)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var outbox = await ctx.OutboxMessages.ToListAsync();
            var alert = outbox.Should().ContainSingle(m => m.Type.Contains("LowStockAlerted")).Subject;
            alert.TenantId.Should().Be(tenantId);
        }
    }

    [Fact]
    public async Task Sale_StaysAboveThreshold_NoLowStockAlerted()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            await IntakeConsumer(ctx).Consume(ContextFor(Consignment(tenantId, productId, 100.000m)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new SetReorderThresholdHandler(
                new StockItemRepository(ctx), new WarehouseRepository(ctx), ctx);
            await handler.Handle(new SetReorderThresholdCommand(productId, 20.000m), CancellationToken.None);
        }

        // Satış 50 → kalan 50 (> 20) → uyarı yok.
        await using (var ctx = CreateContext(stub, dbName))
        {
            await SaleConsumer(ctx).Consume(ContextFor(Sale(tenantId, productId, 50.000m)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var outbox = await ctx.OutboxMessages.ToListAsync();
            outbox.Should().NotContain(m => m.Type.Contains("LowStockAlerted"));
        }
    }

    [Fact]
    public async Task ListLowStock_ReturnsOnlyItemsAtOrBelowThreshold()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var lowProduct = Guid.NewGuid();
        var okProduct = Guid.NewGuid();
        var noThresholdProduct = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            var warehouse = Warehouse.Create(tenantId, "Merkez Depo", "MERKEZ", isDefault: true).Value;
            ctx.Warehouses.Add(warehouse);

            // Düşük: kalan 10, eşik 20 → dahil.
            var low = StockItem.Open(tenantId, warehouse.Id, lowProduct).Value;
            low.RecordIntake(Guid.NewGuid(), 10.000m, DateTime.UtcNow);
            low.SetReorderThreshold(20.000m);
            ctx.StockItems.Add(low);

            // Yeterli: kalan 100, eşik 20 → hariç.
            var ok = StockItem.Open(tenantId, warehouse.Id, okProduct).Value;
            ok.RecordIntake(Guid.NewGuid(), 100.000m, DateTime.UtcNow);
            ok.SetReorderThreshold(20.000m);
            ctx.StockItems.Add(ok);

            // Eşik yok: kalan 1, eşik null → hariç (uyarı devre dışı).
            var none = StockItem.Open(tenantId, warehouse.Id, noThresholdProduct).Value;
            none.RecordIntake(Guid.NewGuid(), 1.000m, DateTime.UtcNow);
            ctx.StockItems.Add(none);

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new ListLowStockHandler(new StockItemRepository(ctx));
            var result = await handler.Handle(new ListLowStockQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle();
            result.Value[0].ProductId.Should().Be(lowProduct);
        }
    }

    [Fact]
    public async Task SpoilageAnalysis_ComputesRatePerProduct_TenTildePercent()
    {
        // docs/06 S2.1: 100 giriş, 10 fire → %10 fire oranı.
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc);
        var within = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

        await using (var ctx = CreateContext(stub, dbName))
        {
            var warehouse = Warehouse.Create(tenantId, "Merkez Depo", "MERKEZ", isDefault: true).Value;
            ctx.Warehouses.Add(warehouse);

            var item = StockItem.Open(tenantId, warehouse.Id, productId).Value;
            item.RecordIntake(Guid.NewGuid(), 100.000m, within);
            item.RecordSpoilage(10.000m, "çürüme", within);
            ctx.StockItems.Add(item);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new SpoilageAnalysisReportHandler(new StockItemRepository(ctx));
            var result = await handler.Handle(new SpoilageAnalysisReportQuery(from, to), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var row = result.Value.Items.Should().ContainSingle().Subject;
            row.ProductId.Should().Be(productId);
            row.TotalIntake.Should().Be(100.000m);
            row.TotalSpoilage.Should().Be(10.000m);
            row.SpoilageRatePercent.Should().Be(10.00m); // %10
        }
    }

    [Fact]
    public async Task SpoilageAnalysis_ZeroIntake_RateIsZero_NoDivideByZero()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc);
        var within = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

        await using (var ctx = CreateContext(stub, dbName))
        {
            var warehouse = Warehouse.Create(tenantId, "Merkez Depo", "MERKEZ", isDefault: true).Value;
            ctx.Warehouses.Add(warehouse);

            // Giriş aralık DIŞINDA (haziran), fire aralık İÇİNDE → giriş 0, fire var → oran 0.
            var item = StockItem.Open(tenantId, warehouse.Id, productId).Value;
            item.RecordIntake(Guid.NewGuid(), 50.000m, new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));
            item.RecordSpoilage(5.000m, "çürüme", within);
            ctx.StockItems.Add(item);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var handler = new SpoilageAnalysisReportHandler(new StockItemRepository(ctx));
            var result = await handler.Handle(new SpoilageAnalysisReportQuery(from, to), CancellationToken.None);

            var row = result.Value.Items.Should().ContainSingle().Subject;
            row.TotalIntake.Should().Be(0m);
            row.TotalSpoilage.Should().Be(5.000m);
            row.SpoilageRatePercent.Should().Be(0m); // sıfıra bölme yok
        }
    }
}

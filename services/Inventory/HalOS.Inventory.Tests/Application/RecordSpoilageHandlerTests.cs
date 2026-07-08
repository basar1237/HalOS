using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Features.RecordSpoilage;
using HalOS.Inventory.Domain.Aggregates;
using HalOS.Inventory.Domain.Enums;
using HalOS.Inventory.Infrastructure.Persistence;
using HalOS.Inventory.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Inventory.Tests.Application;

/// <summary>
/// RecordSpoilage handler testleri (docs/03 M9 / BK-7; docs/02 §57/§237). Gerçek InventoryDbContext
/// (InMemory) + gerçek StockItemRepository ile: fire kalanı azaltır ve SpoilageRecorded event'ini
/// tenant'lı olarak outbox'a atomik yazar (docs/04 §10); BK-7 fire mevcut stoğu aşamaz (negatif engellenir).
/// </summary>
public sealed class RecordSpoilageHandlerTests
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

    private static async Task SeedStockAsync(InventoryDbContext ctx, Guid tenantId, Guid productId, decimal quantity)
    {
        var item = StockItem.Open(tenantId, productId).Value;
        item.RecordIntake(Guid.NewGuid(), quantity, new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc));
        ctx.StockItems.Add(item);
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_RecordsSpoilage_QuantityDecreases_AndWritesEventToOutboxWithTenant()
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
            var handler = new RecordSpoilageHandler(new StockItemRepository(ctx), ctx);
            var result = await handler.Handle(
                new RecordSpoilageCommand(productId, 20.000m, "çürüme", new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc)),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var item = await ctx.StockItems.Include(i => i.Movements).FirstAsync(i => i.ProductId == productId);
            item.QuantityOnHand.Should().Be(80.000m); // 100 − 20
            item.Movements.Should().Contain(m => m.Kind == StockMovementKind.Spoilage);

            var outbox = await ctx.OutboxMessages.ToListAsync();
            outbox.Should().Contain(m => m.Type.Contains("SpoilageRecorded"));
            outbox.First(m => m.Type.Contains("SpoilageRecorded")).TenantId.Should().Be(tenantId);
        }
    }

    [Fact]
    public async Task Handle_SpoilageExceedingStock_Fails_BK7_NoNegative()
    {
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
            var handler = new RecordSpoilageHandler(new StockItemRepository(ctx), ctx);
            var result = await handler.Handle(
                new RecordSpoilageCommand(productId, 11.000m, "çürüme", new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc)),
                CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(StockItemErrors.InsufficientStock);
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var item = await ctx.StockItems.Include(i => i.Movements).FirstAsync(i => i.ProductId == productId);
            item.QuantityOnHand.Should().Be(10.000m); // değişmedi
            item.Movements.Should().NotContain(m => m.Kind == StockMovementKind.Spoilage);
            (await ctx.OutboxMessages.ToListAsync()).Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Handle_MissingStockItem_Fails_NotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        var handler = new RecordSpoilageHandler(new StockItemRepository(ctx), ctx);

        var result = await handler.Handle(
            new RecordSpoilageCommand(Guid.NewGuid(), 5.000m, "çürüme", DateTime.UtcNow),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StockItemErrors.NotFound);
    }
}

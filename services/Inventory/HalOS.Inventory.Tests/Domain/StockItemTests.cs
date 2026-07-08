using FluentAssertions;
using HalOS.Inventory.Domain.Aggregates;
using HalOS.Inventory.Domain.Enums;
using HalOS.Inventory.Domain.Events;
using Xunit;

namespace HalOS.Inventory.Tests.Domain;

/// <summary>
/// StockItem çekirdek aggregate testleri (docs/02 §115 Stok &amp; Depo; docs/03 M9/BK-7).
/// Kalan = Σ hareket; giriş (+) / satış çıkışı (−) / fire (−) yönleri; BK-7 kalan negatif olamaz
/// (çıkış/fire mevcut stoğu aşamaz); idempotency (aynı kaynak iki kez işlenmez). Saf, in-memory.
/// </summary>
public sealed class StockItemTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    private static readonly DateTime OccurredAt = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);

    private StockItem NewItem() => StockItem.Open(_tenantId, _warehouseId, _productId).Value;

    [Fact]
    public void Open_MissingProduct_Fails()
    {
        StockItem.Open(_tenantId, _warehouseId, Guid.Empty).Error
            .Should().Be(StockItemErrors.ProductRequired);
    }

    [Fact]
    public void Open_MissingWarehouse_Fails()
    {
        StockItem.Open(_tenantId, Guid.Empty, _productId).Error
            .Should().Be(StockItemErrors.WarehouseRequired);
    }

    [Fact]
    public void Open_NewItem_StartsWithZeroQuantityAndNoMovements()
    {
        var item = NewItem();

        item.QuantityOnHand.Should().Be(0m);
        item.Movements.Should().BeEmpty();
        item.ProductId.Should().Be(_productId);
        item.TenantId.Should().Be(_tenantId);
        item.WarehouseId.Should().Be(_warehouseId);
        item.ReorderThreshold.Should().BeNull();
    }

    [Fact]
    public void RecordIntake_IncreasesQuantity()
    {
        // docs/02 §229: ConsignmentReceived → stok girişi (kalan artar, +).
        var item = NewItem();

        var result = item.RecordIntake(Guid.NewGuid(), 100.000m, OccurredAt);

        result.IsSuccess.Should().BeTrue();
        item.QuantityOnHand.Should().Be(100.000m);
        item.Movements.Should().ContainSingle();
        item.Movements.Single().Kind.Should().Be(StockMovementKind.Intake);
        item.Movements.Single().SignedQuantity.Should().Be(100.000m);
    }

    [Fact]
    public void RecordSaleOut_DecreasesQuantity()
    {
        // docs/02 §230: SaleCompleted → stok çıkışı (kalan azalır, −).
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 100.000m, OccurredAt);

        var result = item.RecordSaleOut(Guid.NewGuid(), 40.000m, OccurredAt);

        result.IsSuccess.Should().BeTrue();
        item.QuantityOnHand.Should().Be(60.000m);
        item.Movements.Should().HaveCount(2);
        item.Movements.Single(m => m.Kind == StockMovementKind.SaleOut).SignedQuantity.Should().Be(-40.000m);
    }

    [Fact]
    public void QuantityOnHand_IsSumOfMovements_IntakePlusOutMinus()
    {
        // Değişmez: kalan = Σ SignedQuantity (giriş +, çıkış −) (docs/02 §115).
        var item = NewItem();

        item.RecordIntake(Guid.NewGuid(), 100.000m, OccurredAt);   // +100
        item.RecordSaleOut(Guid.NewGuid(), 30.000m, OccurredAt);   // −30
        item.RecordSpoilage(10.000m, "çürüme", OccurredAt);        // −10

        item.QuantityOnHand.Should().Be(60.000m);
        item.Movements.Should().HaveCount(3);
    }

    [Fact]
    public void RecordSpoilage_DecreasesQuantity_AndRaisesSpoilageRecorded()
    {
        // docs/02 §57 Fire=Spoilage; §237 SpoilageRecorded → Finans/AI.
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 100.000m, OccurredAt);

        var result = item.RecordSpoilage(15.000m, "ezilme", OccurredAt);

        result.IsSuccess.Should().BeTrue();
        item.QuantityOnHand.Should().Be(85.000m);
        item.Movements.Single(m => m.Kind == StockMovementKind.Spoilage).SignedQuantity.Should().Be(-15.000m);

        var evt = item.DomainEvents.OfType<SpoilageRecorded>().Should().ContainSingle().Subject;
        evt.Quantity.Should().Be(15.000m);
        evt.Reason.Should().Be("ezilme");
        evt.ProductId.Should().Be(_productId);
        evt.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void RecordSaleOut_ExceedingStock_Fails_BK7_QuantityNotNegative()
    {
        // BK-7: stok çıkışı mevcut stoğu aşamaz (kalan negatif olamaz).
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 50.000m, OccurredAt);

        var result = item.RecordSaleOut(Guid.NewGuid(), 50.001m, OccurredAt);

        result.Error.Should().Be(StockItemErrors.InsufficientStock);
        item.QuantityOnHand.Should().Be(50.000m); // değişmedi
        item.Movements.Should().ContainSingle(); // çıkış eklenmedi
    }

    [Fact]
    public void RecordSpoilage_ExceedingStock_Fails_BK7_QuantityNotNegative()
    {
        // BK-7: fire mevcut stoğu aşamaz (kalan negatif olamaz).
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 10.000m, OccurredAt);

        var result = item.RecordSpoilage(11.000m, "çürüme", OccurredAt);

        result.Error.Should().Be(StockItemErrors.InsufficientStock);
        item.QuantityOnHand.Should().Be(10.000m);
        item.Movements.Should().ContainSingle();
        item.DomainEvents.OfType<SpoilageRecorded>().Should().BeEmpty();
    }

    [Fact]
    public void RecordSpoilage_MissingReason_Fails()
    {
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 10.000m, OccurredAt);

        item.RecordSpoilage(5.000m, "  ", OccurredAt).Error
            .Should().Be(StockItemErrors.SpoilageReasonRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RecordIntake_NonPositiveQuantity_Fails(decimal quantity)
    {
        var item = NewItem();

        item.RecordIntake(Guid.NewGuid(), quantity, OccurredAt).Error
            .Should().Be(StockItemErrors.NonPositiveQuantity);
        item.Movements.Should().BeEmpty();
    }

    [Fact]
    public void RecordIntake_SameConsignmentItemTwice_IsIdempotent_NoDuplicate()
    {
        // docs/04 §5: en-az-bir-kez teslimatta consumer yeniden tetiklenebilir; aynı kaynak (giriş)
        // ikinci kez hareket eklememelidir.
        var item = NewItem();
        var consignmentItemId = Guid.NewGuid();

        item.RecordIntake(consignmentItemId, 100.000m, OccurredAt);
        var second = item.RecordIntake(consignmentItemId, 100.000m, OccurredAt);

        second.IsSuccess.Should().BeTrue();
        item.Movements.Should().ContainSingle();
        item.QuantityOnHand.Should().Be(100.000m);
    }

    [Fact]
    public void RecordSaleOut_SameSaleLineTwice_IsIdempotent_NoDuplicate()
    {
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 100.000m, OccurredAt);
        var saleLineId = Guid.NewGuid();

        item.RecordSaleOut(saleLineId, 40.000m, OccurredAt);
        var second = item.RecordSaleOut(saleLineId, 40.000m, OccurredAt);

        second.IsSuccess.Should().BeTrue();
        item.Movements.Count(m => m.Kind == StockMovementKind.SaleOut).Should().Be(1);
        item.QuantityOnHand.Should().Be(60.000m);
    }

    [Fact]
    public void SetReorderThreshold_NegativeThreshold_Fails()
    {
        var item = NewItem();

        item.SetReorderThreshold(-1m).Error
            .Should().Be(StockItemErrors.NegativeReorderThreshold);
    }

    [Fact]
    public void RecordSaleOut_DropsToOrBelowThreshold_RaisesLowStockAlerted()
    {
        // docs/06 S2.1 stok uyarıları: kalan eşiğe/altına inince LowStockAlerted (eşik geçişinde).
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 100.000m, OccurredAt);
        item.SetReorderThreshold(20.000m);

        // 100 → 15 (eşik 20'nin altına iner) → uyarı.
        item.RecordSaleOut(Guid.NewGuid(), 85.000m, OccurredAt);

        var evt = item.DomainEvents.OfType<LowStockAlerted>().Should().ContainSingle().Subject;
        evt.QuantityOnHand.Should().Be(15.000m);
        evt.ReorderThreshold.Should().Be(20.000m);
        evt.ProductId.Should().Be(_productId);
        evt.WarehouseId.Should().Be(_warehouseId);
        evt.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void RecordSaleOut_StaysAboveThreshold_DoesNotRaiseLowStockAlerted()
    {
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 100.000m, OccurredAt);
        item.SetReorderThreshold(20.000m);

        // 100 → 50 (eşiğin üstünde kalır) → uyarı YOK.
        item.RecordSaleOut(Guid.NewGuid(), 50.000m, OccurredAt);

        item.DomainEvents.OfType<LowStockAlerted>().Should().BeEmpty();
    }

    [Fact]
    public void RecordSaleOut_NullThreshold_NeverRaisesLowStockAlerted()
    {
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 100.000m, OccurredAt);
        // Eşik ayarlanmadı (null) → hiç uyarı yok, kalan 0'a inse bile.
        item.RecordSaleOut(Guid.NewGuid(), 100.000m, OccurredAt);

        item.DomainEvents.OfType<LowStockAlerted>().Should().BeEmpty();
    }

    [Fact]
    public void RecordSaleOut_AlreadyBelowThreshold_DoesNotRaiseAgain_OnlyOnCrossing()
    {
        // Eşik geçişi bir kez: kalan zaten eşiğin altındayken tekrar çıkış olursa yeniden uyarı yok.
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 100.000m, OccurredAt);
        item.SetReorderThreshold(20.000m);

        item.RecordSaleOut(Guid.NewGuid(), 85.000m, OccurredAt); // 100 → 15: uyarı (geçiş)
        item.ClearDomainEvents();

        item.RecordSaleOut(Guid.NewGuid(), 5.000m, OccurredAt); // 15 → 10: zaten altında, yeni uyarı yok

        item.DomainEvents.OfType<LowStockAlerted>().Should().BeEmpty();
    }

    [Fact]
    public void RecordSpoilage_DropsToOrBelowThreshold_RaisesLowStockAlerted()
    {
        var item = NewItem();
        item.RecordIntake(Guid.NewGuid(), 100.000m, OccurredAt);
        item.SetReorderThreshold(20.000m);

        // 100 → 20 (eşiğe iner, dahil) → fire uyarısı + SpoilageRecorded.
        item.RecordSpoilage(80.000m, "çürüme", OccurredAt);

        item.DomainEvents.OfType<LowStockAlerted>().Should().ContainSingle();
        item.DomainEvents.OfType<SpoilageRecorded>().Should().ContainSingle();
    }
}

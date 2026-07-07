using FluentAssertions;
using HalOS.BuildingBlocks.Contracts;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;
using HalOS.Sales.Domain.Events;
using HalOS.Sales.Domain.ValueObjects;
using Xunit;

namespace HalOS.Sales.Tests.Domain;

/// <summary>
/// SaleTransaction çekirdek aggregate testleri (docs/02 §3.3, docs/03 §4 BK-1/BK-2/BK-3/BK-9).
/// Complete → CommissionCalculation + Deduction'lar + Settlement + SaleCompleted event; Cancel
/// tamamlanmış satışı silmez (BK-9). Saf, in-memory.
/// </summary>
public sealed class SaleTransactionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _producerId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private static readonly DateTime SoldAt = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);

    private SaleTransaction NewDraft(bool isWithinMarket = true) =>
        SaleTransaction.Create(
            _tenantId, _buyerId, _producerId, consignmentId: null,
            SoldAt, isWithinMarket, operationId: Guid.NewGuid(), createdBy: _userId).Value;

    private static RateSet WithinMarketRates() =>
        RateSet.Create(0.08m, 0.02m, 0.01m, isWithinMarket: true, vatRate: 0.20m).Value;

    [Fact]
    public void Create_Draft_StartsWithZeroGrossAndDraftStatus()
    {
        var sale = NewDraft();

        sale.Status.Should().Be(SaleStatus.Draft);
        sale.GrossAmount.Should().Be(0m);
        sale.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void AddLine_MultipleLines_GrossIsSumOfLineAmounts()
    {
        // docs/02 §3.3 değişmez: gross = Σ SaleLine.LineAmount.
        var sale = NewDraft();

        sale.AddLine(_productId, quantity: 10m, UnitOfMeasure.Kilogram, unitPrice: 4.50m).IsSuccess.Should().BeTrue(); // 45.00
        sale.AddLine(_productId, quantity: 3m, UnitOfMeasure.Crate, unitPrice: 20m).IsSuccess.Should().BeTrue();       // 60.00

        sale.GrossAmount.Should().Be(105.00m);
        sale.Lines.Should().HaveCount(2);
        sale.Lines.Sum(l => l.LineAmount).Should().Be(105.00m);
    }

    [Fact]
    public void AddLine_InvalidQuantity_Fails()
    {
        var sale = NewDraft();

        sale.AddLine(_productId, 0m, UnitOfMeasure.Kilogram, 5m).IsFailure.Should().BeTrue();
        sale.AddLine(_productId, -1m, UnitOfMeasure.Kilogram, 5m).Error.Should().Be(SaleErrors.InvalidQuantity);
    }

    [Fact]
    public void AddLine_NegativeUnitPrice_Fails()
    {
        var sale = NewDraft();

        sale.AddLine(_productId, 1m, UnitOfMeasure.Kilogram, -0.01m).Error.Should().Be(SaleErrors.InvalidUnitPrice);
    }

    [Fact]
    public void Complete_BK1_HundredGrossWithinMarket_ProducesEightyEightNetAndDeductions()
    {
        // ÇEKİRDEK: BK-1 uçtan uca. gross=100, hal içi, komisyon %8 → net 88,00.
        var sale = NewDraft(isWithinMarket: true);
        sale.AddLine(_productId, quantity: 100m, UnitOfMeasure.Kilogram, unitPrice: 1m); // gross = 100

        var result = sale.Complete(WithinMarketRates());

        result.IsSuccess.Should().BeTrue();
        sale.Status.Should().Be(SaleStatus.Completed);
        sale.GrossAmount.Should().Be(100.00m);

        // Settlement.net = 88,00 (BK-1 birebir).
        sale.Settlement.Should().NotBeNull();
        sale.Settlement!.NetAmount.Should().Be(88.00m);

        // Komisyon hesabı: 8,00 + KDV 1,60 (hakedişten düşülmez).
        sale.CommissionCalculation.Should().NotBeNull();
        sale.CommissionCalculation!.CommissionAmount.Should().Be(8.00m);
        sale.CommissionCalculation.VatAmount.Should().Be(1.60m);

        // Kesinti kalemleri AYRI (docs/02 §7): commission/agri/ssk/market_fee/vat.
        sale.Deductions.Should().HaveCount(5);
        sale.Deductions.Single(d => d.Type == DeductionType.Commission).Amount.Should().Be(8.00m);
        sale.Deductions.Single(d => d.Type == DeductionType.AgriWithholding).Amount.Should().Be(2.00m);
        sale.Deductions.Single(d => d.Type == DeductionType.FarmerSsk).Amount.Should().Be(1.00m);
        sale.Deductions.Single(d => d.Type == DeductionType.MarketFee).Amount.Should().Be(1.00m);
        sale.Deductions.Single(d => d.Type == DeductionType.Vat).Amount.Should().Be(1.60m);
    }

    [Fact]
    public void Complete_RaisesSaleCompletedEvent_WithNetAndDueDate()
    {
        var sale = NewDraft();
        sale.AddLine(_productId, 100m, UnitOfMeasure.Kilogram, 1m);

        sale.Complete(WithinMarketRates());

        var evt = sale.DomainEvents.OfType<SaleCompleted>().Should().ContainSingle().Subject;
        evt.SaleTransactionId.Should().Be(sale.Id);
        evt.TenantId.Should().Be(_tenantId);
        evt.NetAmount.Should().Be(88.00m);
        evt.GrossAmount.Should().Be(100.00m);
        evt.CommissionAmount.Should().Be(8.00m);
        // e-MM kırılımı: yalnız stopaj + Bağ-Kur taşınır (Integration servisi e-MM için kullanır).
        evt.AgriWithholdingAmount.Should().Be(2.00m);
        evt.FarmerSskAmount.Should().Be(1.00m);
        evt.TotalDeductions.Should().Be(12.00m); // KDV hariç.
        // Vade = satış + 15 iş günü (BK-3): 2026-07-06 Pzt başlangıç; aralıkta 15 Temmuz
        // (Demokrasi ve Millî Birlik Günü) resmi tatili atlanır → 2026-07-28 Salı.
        evt.SettlementDueDate.Date.Should().Be(new DateTime(2026, 7, 28));
    }

    [Fact]
    public void Complete_SettlementDueDate_IsFifteenBusinessDays_WeekendSkipped()
    {
        var sale = NewDraft();
        sale.AddLine(_productId, 100m, UnitOfMeasure.Kilogram, 1m);

        sale.Complete(WithinMarketRates());

        var expected = BusinessDayCalculator.AddBusinessDays(SoldAt, SaleTransaction.SettlementDueBusinessDays);
        sale.Settlement!.DueDate.Should().Be(expected);
        sale.Settlement.DueDate.DayOfWeek.Should().NotBe(DayOfWeek.Saturday);
        sale.Settlement.DueDate.DayOfWeek.Should().NotBe(DayOfWeek.Sunday);
    }

    [Fact]
    public void Create_DefaultTerm_IsCash()
    {
        NewDraft().Term.Should().Be(SaleTerm.Cash);
    }

    [Fact]
    public void Complete_DeferredTerm_SettlementDueDate_IsThirtyCalendarDays()
    {
        // Vadeli satış (BK-3): ödeme 30 TAKVİM günü içinde (iş günü değil — hafta sonu/tatil atlanmaz).
        var sale = SaleTransaction.Create(
            _tenantId, _buyerId, _producerId, consignmentId: null,
            SoldAt, isWithinMarket: true, operationId: Guid.NewGuid(), createdBy: _userId,
            term: SaleTerm.Deferred).Value;
        sale.AddLine(_productId, 100m, UnitOfMeasure.Kilogram, 1m);

        var result = sale.Complete(WithinMarketRates());

        result.IsSuccess.Should().BeTrue();
        // 2026-07-06 + 30 takvim günü = 2026-08-05 (iş günü hesabı YAPILMAZ).
        sale.Settlement!.DueDate.Should().Be(SoldAt.AddDays(SaleTransaction.DeferredDueCalendarDays));
        sale.Settlement.DueDate.Date.Should().Be(new DateTime(2026, 8, 5));

        var evt = sale.DomainEvents.OfType<SaleCompleted>().Single();
        evt.SettlementDueDate.Should().Be(SoldAt.AddDays(SaleTransaction.DeferredDueCalendarDays));
    }

    [Fact]
    public void Complete_NoLines_Fails()
    {
        var sale = NewDraft();

        sale.Complete(WithinMarketRates()).Error.Should().Be(SaleErrors.NoLines);
    }

    [Fact]
    public void Complete_AlreadyCompleted_Fails()
    {
        var sale = NewDraft();
        sale.AddLine(_productId, 100m, UnitOfMeasure.Kilogram, 1m);
        sale.Complete(WithinMarketRates());

        sale.Complete(WithinMarketRates()).Error.Should().Be(SaleErrors.AlreadyCompleted);
    }

    [Fact]
    public void Complete_SettlementNet_IsNeverNegative()
    {
        // Değişmez: net negatif olamaz. Normal oranlarla küçük tutarda dahi net ≥ 0.
        var sale = NewDraft();
        sale.AddLine(_productId, 1m, UnitOfMeasure.Piece, 0.10m); // gross = 0.10

        var result = sale.Complete(WithinMarketRates());

        result.IsSuccess.Should().BeTrue();
        sale.Settlement!.NetAmount.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void AddLine_AfterCompleted_Fails()
    {
        var sale = NewDraft();
        sale.AddLine(_productId, 100m, UnitOfMeasure.Kilogram, 1m);
        sale.Complete(WithinMarketRates());

        sale.AddLine(_productId, 5m, UnitOfMeasure.Kilogram, 1m).Error.Should().Be(SaleErrors.NotDraft);
    }

    [Fact]
    public void Cancel_CompletedSale_IsNotDeleted_FlagAndStatusSet_BK9()
    {
        // BK-9: tamamlanmış satış SİLİNMEZ; iptal ters kayıt/flag ile. Satırlar/hakediş korunur.
        var sale = NewDraft();
        sale.AddLine(_productId, 100m, UnitOfMeasure.Kilogram, 1m);
        sale.Complete(WithinMarketRates());

        var result = sale.Cancel("Alıcı vazgeçti");

        result.IsSuccess.Should().BeTrue();
        sale.Status.Should().Be(SaleStatus.Cancelled);
        sale.IsCancelled.Should().BeTrue();
        sale.CancellationReason.Should().Be("Alıcı vazgeçti");

        // Denetim izi: satırlar ve hakediş hâlâ mevcut (silinmedi).
        sale.Lines.Should().NotBeEmpty();
        sale.Settlement.Should().NotBeNull();

        // SaleCancelled event yayınlandı.
        sale.DomainEvents.OfType<SaleCancelled>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Fails()
    {
        var sale = NewDraft();
        sale.AddLine(_productId, 100m, UnitOfMeasure.Kilogram, 1m);
        sale.Cancel("sebep");

        sale.Cancel("tekrar").Error.Should().Be(SaleErrors.AlreadyCancelled);
    }

    [Fact]
    public void Complete_CancelledSale_Fails()
    {
        var sale = NewDraft();
        sale.AddLine(_productId, 100m, UnitOfMeasure.Kilogram, 1m);
        sale.Cancel("sebep");

        sale.Complete(WithinMarketRates()).Error.Should().Be(SaleErrors.CancelledSaleCannotComplete);
    }

    [Fact]
    public void Create_MissingBuyer_Fails()
    {
        var result = SaleTransaction.Create(
            _tenantId, Guid.Empty, _producerId, null, SoldAt, true, Guid.NewGuid(), _userId);

        result.Error.Should().Be(SaleErrors.BuyerRequired);
    }
}

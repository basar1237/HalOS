using FluentAssertions;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;
using HalOS.Integration.Domain.Events;
using Xunit;

namespace HalOS.Integration.Tests.Domain;

/// <summary>
/// HksNotification (HKS bildirimi) aggregate testleri (docs/02 §3.5, docs/03 BK-4/BK-5). Saf, in-memory.
/// Brüt + komisyon + hal rüsumu AYRI taşınır (docs/02 §7); tutarlar SaleCompleted'tan gelir (yeniden
/// hesap yok). ProducerReceiptTests deseniyle birebir.
/// </summary>
public sealed class HksNotificationTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTime NotifiedDate = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);

    private static Result<HksNotification> Sample(decimal gross = 100m, decimal commission = 8m, decimal marketFee = 1m) =>
        HksNotification.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), NotifiedDate, gross, commission, marketFee);

    [Fact]
    public void Create_CarriesGrossCommissionAndMarketFee_Separately()
    {
        // BK-5 / docs/02 §7: rüsum AYRI taşınır (tek "fee" altında birleştirilmez).
        var hks = Sample(gross: 100m, commission: 8m, marketFee: 1m).Value;

        hks.GrossAmount.Should().Be(100.00m);
        hks.CommissionAmount.Should().Be(8.00m);
        hks.MarketFeeAmount.Should().Be(1.00m);
        hks.Status.Should().Be(HksNotificationStatus.Draft);
    }

    [Fact]
    public void Create_NonPositiveGross_Fails()
    {
        Sample(gross: 0m).Error.Should().Be(HksNotification.HksNotificationErrors.NonPositiveGross);
    }

    [Fact]
    public void Create_NegativeMarketFee_Fails()
    {
        Sample(gross: 100m, marketFee: -0.01m).Error
            .Should().Be(HksNotification.HksNotificationErrors.NegativeAmount);
    }

    [Fact]
    public void MarkNotified_SetsReferenceAndStatus_AndRaisesEvent()
    {
        var hks = Sample().Value;

        var result = hks.MarkNotified("HKS-20260706-ABCD1234");

        result.IsSuccess.Should().BeTrue();
        hks.Status.Should().Be(HksNotificationStatus.Notified);
        hks.ReferenceNumber.Should().Be("HKS-20260706-ABCD1234");

        var evt = hks.DomainEvents.OfType<HksNotified>().Should().ContainSingle().Subject;
        evt.SaleTransactionId.Should().Be(hks.SaleTransactionId);
        evt.TenantId.Should().Be(Tenant);
        evt.ReferenceNumber.Should().Be("HKS-20260706-ABCD1234");
        evt.GrossAmount.Should().Be(100.00m);
        evt.MarketFeeAmount.Should().Be(1.00m);
    }

    [Fact]
    public void MarkNotified_Twice_IsIdempotent_NoSecondEvent()
    {
        var hks = Sample().Value;
        hks.MarkNotified("HKS-1");
        hks.ClearDomainEvents();

        var second = hks.MarkNotified("HKS-2");

        second.IsSuccess.Should().BeTrue();
        hks.Status.Should().Be(HksNotificationStatus.Notified);
        hks.ReferenceNumber.Should().Be("HKS-1"); // ilk referans korunur (idempotent)
        hks.DomainEvents.OfType<HksNotified>().Should().BeEmpty();
    }
}

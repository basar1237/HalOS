using FluentAssertions;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;
using HalOS.Integration.Domain.Events;
using Xunit;

namespace HalOS.Integration.Tests.Domain;

/// <summary>
/// ProducerReceipt (e-MM) aggregate testleri (docs/02 §1.3/§3.5, docs/03 BK-4). Saf, in-memory.
/// e-MM YALNIZ stopaj + Bağ-Kur içerir; net = brüt − (stopaj + Bağ-Kur); komisyon/rüsum/KDV girmez.
/// </summary>
public sealed class ProducerReceiptTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTime IssueDate = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);

    private static Result<ProducerReceipt> Sample(decimal gross = 100m, decimal agri = 2m, decimal ssk = 1m) =>
        ProducerReceipt.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), IssueDate, gross, agri, ssk);

    [Fact]
    public void Create_Net_IsGrossMinusStopajAndSsk_WithTwoDeductionLines()
    {
        // BK-1/BK-4: e-MM net = brüt − (stopaj + Bağ-Kur). Komisyon/rüsum/KDV GİRMEZ.
        var receipt = Sample(gross: 100m, agri: 2m, ssk: 1m).Value;

        receipt.GrossAmount.Should().Be(100.00m);
        receipt.AgriWithholdingAmount.Should().Be(2.00m);
        receipt.FarmerSskAmount.Should().Be(1.00m);
        receipt.NetPayable.Should().Be(97.00m); // 100 − 2 − 1 (komisyon/rüsum DAHİL DEĞİL)
        receipt.Status.Should().Be(ProducerReceiptStatus.Draft);

        receipt.Deductions.Should().HaveCount(2);
        receipt.Deductions.Single(d => d.Type == ReceiptDeductionType.AgriWithholding).Amount.Should().Be(2.00m);
        receipt.Deductions.Single(d => d.Type == ReceiptDeductionType.FarmerSsk).Amount.Should().Be(1.00m);
    }

    [Fact]
    public void Create_NonPositiveGross_Fails()
    {
        Sample(gross: 0m).Error.Should().Be(ProducerReceipt.ProducerReceiptErrors.NonPositiveGross);
    }

    [Fact]
    public void Create_DeductionsExceedGross_NegativeNet_Fails()
    {
        // Bozuk event koruması: kesintiler brütü aşamaz (net negatif olamaz, BK-1).
        Sample(gross: 100m, agri: 80m, ssk: 30m).Error
            .Should().Be(ProducerReceipt.ProducerReceiptErrors.NegativeNet);
    }

    [Fact]
    public void MarkIssued_SetsNumberAndStatus_AndRaisesEvent()
    {
        var receipt = Sample().Value;

        var result = receipt.MarkIssued("EMM-20260706-ABCD1234");

        result.IsSuccess.Should().BeTrue();
        receipt.Status.Should().Be(ProducerReceiptStatus.Issued);
        receipt.ReceiptNumber.Should().Be("EMM-20260706-ABCD1234");

        var evt = receipt.DomainEvents.OfType<ProducerReceiptIssued>().Should().ContainSingle().Subject;
        evt.SaleTransactionId.Should().Be(receipt.SaleTransactionId);
        evt.TenantId.Should().Be(Tenant);
        evt.ReceiptNumber.Should().Be("EMM-20260706-ABCD1234");
        evt.NetPayable.Should().Be(97.00m);
    }

    [Fact]
    public void MarkIssued_Twice_IsIdempotent_NoSecondEvent()
    {
        var receipt = Sample().Value;
        receipt.MarkIssued("EMM-1");
        receipt.ClearDomainEvents();

        var second = receipt.MarkIssued("EMM-2");

        second.IsSuccess.Should().BeTrue();
        receipt.Status.Should().Be(ProducerReceiptStatus.Issued);
        receipt.ReceiptNumber.Should().Be("EMM-1"); // ilk numara korunur (idempotent)
        receipt.DomainEvents.OfType<ProducerReceiptIssued>().Should().BeEmpty();
    }
}

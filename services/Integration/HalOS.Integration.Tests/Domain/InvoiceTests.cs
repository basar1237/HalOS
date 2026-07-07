using FluentAssertions;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;
using HalOS.Integration.Domain.Events;
using Xunit;

namespace HalOS.Integration.Tests.Domain;

/// <summary>
/// Invoice (e-Fatura HAL) aggregate testleri (docs/02 §1.2/§3.5, docs/03 BK-4). Saf, in-memory.
/// e-Fatura ALICIYA kesilir; senaryo = HAL, tür = KOMİSYON; toplam = komisyon + komisyon KDV'si
/// (yeniden hesap yok — SaleCompleted taşır). ProducerReceiptTests deseniyle birebir.
/// </summary>
public sealed class InvoiceTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTime IssueDate = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);

    private static Result<Invoice> Sample(decimal commission = 8m, decimal vat = 1.60m) =>
        Invoice.CreateCommission(Tenant, Guid.NewGuid(), Guid.NewGuid(), IssueDate, commission, vat);

    [Fact]
    public void CreateCommission_HalScenario_CommissionType_TotalIsCommissionPlusVat()
    {
        // BK-4 / docs/02 §1.2: e-Fatura HAL/KOMİSYON, toplam = komisyon 8,00 + KDV 1,60 = 9,60.
        var invoice = Sample(commission: 8m, vat: 1.60m).Value;

        invoice.Scenario.Should().Be(InvoiceScenario.Hal);
        invoice.Type.Should().Be(InvoiceType.Commission);
        invoice.CommissionAmount.Should().Be(8.00m);
        invoice.CommissionVatAmount.Should().Be(1.60m);
        invoice.TotalAmount.Should().Be(9.60m);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void CreateCommission_NonPositiveCommission_Fails()
    {
        Sample(commission: 0m).Error.Should().Be(Invoice.InvoiceErrors.NonPositiveCommission);
    }

    [Fact]
    public void CreateCommission_NegativeVat_Fails()
    {
        Sample(commission: 8m, vat: -0.01m).Error.Should().Be(Invoice.InvoiceErrors.NegativeVat);
    }

    [Fact]
    public void MarkIssued_SetsNumberAndStatus_AndRaisesEvent()
    {
        var invoice = Sample().Value;

        var result = invoice.MarkIssued("EFA-20260706-ABCD1234");

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.InvoiceNumber.Should().Be("EFA-20260706-ABCD1234");

        var evt = invoice.DomainEvents.OfType<InvoiceIssued>().Should().ContainSingle().Subject;
        evt.SaleTransactionId.Should().Be(invoice.SaleTransactionId);
        evt.TenantId.Should().Be(Tenant);
        evt.BuyerPartyId.Should().Be(invoice.BuyerPartyId);
        evt.InvoiceNumber.Should().Be("EFA-20260706-ABCD1234");
        evt.TotalAmount.Should().Be(9.60m);
    }

    [Fact]
    public void MarkIssued_Twice_IsIdempotent_NoSecondEvent()
    {
        var invoice = Sample().Value;
        invoice.MarkIssued("EFA-1");
        invoice.ClearDomainEvents();

        var second = invoice.MarkIssued("EFA-2");

        second.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.InvoiceNumber.Should().Be("EFA-1"); // ilk numara korunur (idempotent)
        invoice.DomainEvents.OfType<InvoiceIssued>().Should().BeEmpty();
    }
}

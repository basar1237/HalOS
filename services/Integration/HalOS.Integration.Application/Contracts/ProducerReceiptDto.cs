using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;

namespace HalOS.Integration.Application.Contracts;

/// <summary>e-MM kesinti kalemi okuma DTO'su (docs/02 §3.5). Domain entity'si API'ye sızmaz.</summary>
public sealed record ReceiptDeductionDto(
    Guid Id,
    ReceiptDeductionType Type,
    decimal Amount)
{
    public static ReceiptDeductionDto FromDomain(ReceiptDeduction deduction) => new(
        deduction.Id,
        deduction.Type,
        deduction.Amount);
}

/// <summary>
/// e-Müstahsil Makbuzu (e-MM) okuma DTO'su (docs/02 §3.5 <c>ProducerReceipt</c>). Net ödenecek =
/// brüt − (stopaj + Bağ-Kur). Domain aggregate'i API'ye sızmaz.
/// </summary>
public sealed record ProducerReceiptDto(
    Guid Id,
    Guid TenantId,
    Guid SaleTransactionId,
    Guid ProducerPartyId,
    Guid BuyerPartyId,
    DateTime IssueDate,
    decimal GrossAmount,
    decimal AgriWithholdingAmount,
    decimal FarmerSskAmount,
    decimal NetPayable,
    string? ReceiptNumber,
    ProducerReceiptStatus Status,
    IReadOnlyList<ReceiptDeductionDto> Deductions)
{
    public static ProducerReceiptDto FromDomain(ProducerReceipt receipt) => new(
        receipt.Id,
        receipt.TenantId,
        receipt.SaleTransactionId,
        receipt.ProducerPartyId,
        receipt.BuyerPartyId,
        receipt.IssueDate,
        receipt.GrossAmount,
        receipt.AgriWithholdingAmount,
        receipt.FarmerSskAmount,
        receipt.NetPayable,
        receipt.ReceiptNumber,
        receipt.Status,
        receipt.Deductions.Select(ReceiptDeductionDto.FromDomain).ToList());
}

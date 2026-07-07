using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;

namespace HalOS.Integration.Application.Contracts;

/// <summary>
/// e-Fatura (HAL / <c>Invoice</c>) okuma DTO'su (docs/02 §1.2/§3.5). Alıcıya kesilir; senaryo = HAL,
/// tür = KOMİSYON. Toplam = komisyon + komisyon KDV'si. Domain aggregate'i API'ye sızmaz.
/// </summary>
public sealed record InvoiceDto(
    Guid Id,
    Guid TenantId,
    Guid SaleTransactionId,
    Guid BuyerPartyId,
    DateTime IssueDate,
    InvoiceScenario Scenario,
    InvoiceType Type,
    decimal CommissionAmount,
    decimal CommissionVatAmount,
    decimal TotalAmount,
    string? InvoiceNumber,
    InvoiceStatus Status)
{
    public static InvoiceDto FromDomain(Invoice invoice) => new(
        invoice.Id,
        invoice.TenantId,
        invoice.SaleTransactionId,
        invoice.BuyerPartyId,
        invoice.IssueDate,
        invoice.Scenario,
        invoice.Type,
        invoice.CommissionAmount,
        invoice.CommissionVatAmount,
        invoice.TotalAmount,
        invoice.InvoiceNumber,
        invoice.Status);
}

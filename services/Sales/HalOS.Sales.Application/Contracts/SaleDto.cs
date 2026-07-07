using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;

namespace HalOS.Sales.Application.Contracts;

/// <summary>Satış satırı okuma DTO'su (docs/05 §3.5 <c>sale_line</c>).</summary>
public sealed record SaleLineDto(
    Guid Id,
    Guid ProductId,
    decimal Quantity,
    UnitOfMeasure Unit,
    decimal UnitPrice,
    decimal LineAmount);

/// <summary>Kesinti kalemi okuma DTO'su (docs/05 §3.5 <c>deduction</c>).</summary>
public sealed record DeductionDto(DeductionType Type, decimal Rate, decimal Amount);

/// <summary>Komisyon hesabı okuma DTO'su (docs/05 §3.5 <c>commission_calculation</c>).</summary>
public sealed record CommissionCalculationDto(
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal VatRate,
    decimal VatAmount);

/// <summary>Hakediş okuma DTO'su (docs/05 §3.5 <c>settlement</c>).</summary>
public sealed record SettlementDto(decimal NetAmount, DateTime DueDate, SettlementStatus Status);

/// <summary>Satış kaydı okuma DTO'su. Domain aggregate'i API'ye sızmaz.</summary>
public sealed record SaleDto(
    Guid Id,
    Guid TenantId,
    Guid BuyerPartyId,
    Guid ProducerPartyId,
    Guid? ConsignmentId,
    DateTime SoldAt,
    decimal GrossAmount,
    bool IsWithinMarket,
    SaleStatus Status,
    Guid OperationId,
    bool IsCancelled,
    string? CancellationReason,
    IReadOnlyList<SaleLineDto> Lines,
    CommissionCalculationDto? CommissionCalculation,
    IReadOnlyList<DeductionDto> Deductions,
    SettlementDto? Settlement)
{
    public static SaleDto FromDomain(SaleTransaction sale) => new(
        sale.Id,
        sale.TenantId,
        sale.BuyerPartyId,
        sale.ProducerPartyId,
        sale.ConsignmentId,
        sale.SoldAt,
        sale.GrossAmount,
        sale.IsWithinMarket,
        sale.Status,
        sale.OperationId,
        sale.IsCancelled,
        sale.CancellationReason,
        sale.Lines
            .Select(l => new SaleLineDto(l.Id, l.ProductId, l.Quantity, l.Unit, l.UnitPrice, l.LineAmount))
            .ToList(),
        sale.CommissionCalculation is null
            ? null
            : new CommissionCalculationDto(
                sale.CommissionCalculation.CommissionRate,
                sale.CommissionCalculation.CommissionAmount,
                sale.CommissionCalculation.VatRate,
                sale.CommissionCalculation.VatAmount),
        sale.Deductions
            .Select(d => new DeductionDto(d.Type, d.Rate, d.Amount))
            .ToList(),
        sale.Settlement is null
            ? null
            : new SettlementDto(sale.Settlement.NetAmount, sale.Settlement.DueDate, sale.Settlement.Status));
}

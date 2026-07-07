using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Domain.Enums;

namespace HalOS.Sales.Application.Features.AddSaleLine;

/// <summary>
/// Taslak satışa bir satır ekler (docs/03 M4). Miktar &gt; 0, birim fiyat ≥ 0 (docs/07 §5).
/// Satır tutarı = miktar × birim fiyat (BK-1) domain'de hesaplanır.
/// </summary>
public sealed record AddSaleLineCommand(
    Guid SaleId,
    Guid ProductId,
    decimal Quantity,
    UnitOfMeasure Unit,
    decimal UnitPrice) : ICommand;

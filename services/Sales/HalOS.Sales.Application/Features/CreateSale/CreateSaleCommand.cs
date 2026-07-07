using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Domain.Enums;

namespace HalOS.Sales.Application.Features.CreateSale;

/// <summary>
/// Yeni bir taslak (Draft) satış kaydı oluşturur (docs/03 M4). Alıcı ve müstahsil referansları
/// zorunlu; <paramref name="OperationId"/> offline idempotency içindir (docs/04 §5) — aynı
/// operationId ile ikinci istek mevcut satışı döndürür. <paramref name="Term"/> ödeme vadesini
/// belirler (peşin 15 iş günü / vadeli 30 gün, BK-3).
/// </summary>
public sealed record CreateSaleCommand(
    Guid BuyerPartyId,
    Guid ProducerPartyId,
    Guid? ConsignmentId,
    DateTime SoldAt,
    bool IsWithinMarket,
    Guid OperationId,
    SaleTerm Term = SaleTerm.Cash) : ICommand<Guid>;

using HalOS.BuildingBlocks.Application;

namespace HalOS.Sales.Application.Features.CreateSale;

/// <summary>
/// Yeni bir taslak (Draft) satış kaydı oluşturur (docs/03 M4). Alıcı ve müstahsil referansları
/// zorunlu; <paramref name="OperationId"/> offline idempotency içindir (docs/04 §5) — aynı
/// operationId ile ikinci istek mevcut satışı döndürür.
/// </summary>
public sealed record CreateSaleCommand(
    Guid BuyerPartyId,
    Guid ProducerPartyId,
    Guid? ConsignmentId,
    DateTime SoldAt,
    bool IsWithinMarket,
    Guid OperationId) : ICommand<Guid>;

using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Domain.Enums;

namespace HalOS.Sales.Application.Features.SyncOfflineSale;

/// <summary>
/// Hal Terminali (offline masaüstü, ADR-005) tarafından üretilen bir satışı TEK idempotent,
/// TEK transaction'da buluta oynatır: create + satırlar + complete atomik yürür (docs/04 §5).
/// <paramref name="OperationId"/> istemci üretimlidir; aynı operationId ile ikinci çağrı yeni
/// satış OLUŞTURMAZ, mevcut satışın Id'sini döndürür (çift senkron güvenli). Kesinti/hakediş
/// motoru <c>SaleTransaction.Complete</c> içinde çalışır (BK-1/BK-2/BK-3); yetkili tutarlar burada
/// hesaplanır — terminalde gösterilen yalnızca tahmindir.
/// </summary>
public sealed record SyncOfflineSaleCommand(
    Guid BuyerPartyId,
    Guid ProducerPartyId,
    Guid? ConsignmentId,
    DateTime SoldAt,
    bool IsWithinMarket,
    Guid OperationId,
    SaleTerm Term,
    IReadOnlyList<OfflineSaleLine> Lines) : ICommand<Guid>;

/// <summary>Offline satışın tek satırı (terminalde yerelde girilmiş).</summary>
public sealed record OfflineSaleLine(
    Guid ProductId,
    decimal Quantity,
    UnitOfMeasure Unit,
    decimal UnitPrice);

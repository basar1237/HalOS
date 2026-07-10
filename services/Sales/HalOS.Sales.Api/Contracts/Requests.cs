using HalOS.Sales.Application.Features.ReceiveConsignment;
using HalOS.Sales.Domain.Enums;

namespace HalOS.Sales.Api.Contracts;

/// <summary>Mal geliş kabul isteği (docs/03 M3).</summary>
public sealed record ReceiveConsignmentRequest(
    Guid ProducerPartyId,
    DateTime ReceivedAt,
    string? DispatchNoteRef,
    IReadOnlyList<ConsignmentItemInput> Items);

/// <summary>
/// Satış oluşturma isteği (docs/03 M4). OperationId offline idempotency içindir (docs/04 §5).
/// Term belirtilmezse peşin (<see cref="SaleTerm.Cash"/>) kabul edilir (BK-3).
/// </summary>
public sealed record CreateSaleRequest(
    Guid BuyerPartyId,
    Guid ProducerPartyId,
    Guid? ConsignmentId,
    DateTime SoldAt,
    bool IsWithinMarket,
    Guid OperationId,
    SaleTerm? Term = null);

/// <summary>Satış satırı ekleme isteği (docs/03 M4).</summary>
public sealed record AddSaleLineRequest(
    Guid ProductId,
    decimal Quantity,
    UnitOfMeasure Unit,
    decimal UnitPrice);

/// <summary>Satış iptal isteği (docs/03 §4 BK-9). Gerekçe denetim izi için.</summary>
public sealed record CancelSaleRequest(string Reason);

/// <summary>
/// Hal Terminali (offline masaüstü, ADR-005) satış senkron isteği (docs/04 §5). Terminal, offline
/// oluşturduğu satışı bağlantı gelince TEK idempotent çağrıyla oynatır (create+satır+complete).
/// <see cref="OperationId"/> istemci üretimli idempotency anahtarıdır; tekrar gönderim çift kayıt
/// oluşturmaz. Term belirtilmezse peşin (BK-3).
/// </summary>
public sealed record SyncOfflineSaleRequest(
    Guid BuyerPartyId,
    Guid ProducerPartyId,
    Guid? ConsignmentId,
    DateTime SoldAt,
    bool IsWithinMarket,
    Guid OperationId,
    IReadOnlyList<OfflineSaleLineInput> Lines,
    SaleTerm? Term = null);

/// <summary>Offline satışın tek satırı (docs/03 M4).</summary>
public sealed record OfflineSaleLineInput(
    Guid ProductId,
    decimal Quantity,
    UnitOfMeasure Unit,
    decimal UnitPrice);

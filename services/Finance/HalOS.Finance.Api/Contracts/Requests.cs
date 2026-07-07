using HalOS.Finance.Domain.Enums;

namespace HalOS.Finance.Api.Contracts;

/// <summary>
/// Müstahsile ödeme isteği (docs/03 M6). Kanal cash/bank; 7.000 TL üstü nakit reddedilir (BK-6).
/// Banka referansı banka kanalında belgeleme içindir (opsiyonel).
/// </summary>
public sealed record RecordPaymentRequest(
    Guid PartyId,
    decimal Amount,
    PaymentChannel Channel,
    string? BankReference,
    DateTime OccurredAt);

/// <summary>Alıcıdan tahsilat isteği (docs/03 M6). Kanal cash/bank; BK-6 nakit eşiği geçerli.</summary>
public sealed record RecordCollectionRequest(
    Guid PartyId,
    decimal Amount,
    PaymentChannel Channel,
    string? BankReference,
    DateTime OccurredAt);

/// <summary>Avans isteği (docs/03 M6; docs/02 §3.4). Kanal cash/bank; BK-6 nakit eşiği geçerli.</summary>
public sealed record RecordAdvanceRequest(
    Guid PartyId,
    decimal Amount,
    PaymentChannel Channel,
    string? BankReference,
    DateTime OccurredAt);

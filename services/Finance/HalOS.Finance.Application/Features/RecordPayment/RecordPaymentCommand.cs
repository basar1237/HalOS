using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Domain.Enums;

namespace HalOS.Finance.Application.Features.RecordPayment;

/// <summary>
/// Müstahsile ödeme kaydeder (docs/02 §3.4 <c>Payment</c>; docs/03 M6). Ödeme, müstahsilin cari
/// ALACAĞINI azaltan bir borç hareketidir. BK-6: 7.000 TL üstü nakit yasak (banka zorunlu) —
/// domain <see cref="Domain.Aggregates.CurrentAccount.RecordPayment"/> içinde uygulanır.
/// Müstahsile ödeme 15 iş günü içinde planlanır (BK-3); vade hakediş hareketinde saklanır.
/// </summary>
/// <param name="PartyId">Ödeme yapılan müstahsil (Party ID).</param>
/// <param name="Amount">Ödeme tutarı (pozitif, decimal — BK-2).</param>
/// <param name="Channel">Ödeme kanalı (cash/bank); 7.000 TL üstü nakit reddedilir (BK-6).</param>
/// <param name="BankReference">Banka referansı (banka kanalında belgeleme için, opsiyonel).</param>
/// <param name="OccurredAt">Ödemenin gerçekleştiği an.</param>
public sealed record RecordPaymentCommand(
    Guid PartyId,
    decimal Amount,
    PaymentChannel Channel,
    string? BankReference,
    DateTime OccurredAt) : ICommand<Guid>;

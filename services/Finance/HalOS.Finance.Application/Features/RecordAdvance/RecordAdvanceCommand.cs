using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Domain.Enums;

namespace HalOS.Finance.Application.Features.RecordAdvance;

/// <summary>
/// Avans (peşin ödeme) kaydeder (docs/02 §3.4 <c>Advance</c>: teslimat/satış öncesi verilen peşin
/// ödeme; ileride mahsuplaşır). Avans, tarafın (müstahsil) cari ALACAĞINI azaltan bir borç
/// hareketidir. BK-6: 7.000 TL üstü nakit yasak (banka zorunlu).
/// </summary>
/// <param name="PartyId">Avans verilen taraf (Party ID).</param>
/// <param name="Amount">Avans tutarı (pozitif, decimal — BK-2).</param>
/// <param name="Channel">Avans kanalı (cash/bank); 7.000 TL üstü nakit reddedilir (BK-6).</param>
/// <param name="BankReference">Banka referansı (opsiyonel).</param>
/// <param name="OccurredAt">Avansın gerçekleştiği an.</param>
public sealed record RecordAdvanceCommand(
    Guid PartyId,
    decimal Amount,
    PaymentChannel Channel,
    string? BankReference,
    DateTime OccurredAt) : ICommand<Guid>;

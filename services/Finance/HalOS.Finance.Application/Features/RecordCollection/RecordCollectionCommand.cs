using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Domain.Enums;

namespace HalOS.Finance.Application.Features.RecordCollection;

/// <summary>
/// Alıcıdan tahsilat kaydeder (docs/02 §3.4 <c>Collection</c>; docs/03 M6). Tahsilat, alıcının cari
/// BORCUNU azaltan bir alacak hareketidir. BK-6: 7.000 TL üstü nakit yasak (banka zorunlu).
/// </summary>
/// <param name="PartyId">Tahsilat yapılan alıcı (Party ID).</param>
/// <param name="Amount">Tahsilat tutarı (pozitif, decimal — BK-2).</param>
/// <param name="Channel">Tahsilat kanalı (cash/bank); 7.000 TL üstü nakit reddedilir (BK-6).</param>
/// <param name="BankReference">Banka referansı (opsiyonel).</param>
/// <param name="OccurredAt">Tahsilatın gerçekleştiği an.</param>
public sealed record RecordCollectionCommand(
    Guid PartyId,
    decimal Amount,
    PaymentChannel Channel,
    string? BankReference,
    DateTime OccurredAt) : ICommand<Guid>;

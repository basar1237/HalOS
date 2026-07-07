using HalOS.BuildingBlocks.Domain;

namespace HalOS.Finance.Domain.Events;

/// <summary>
/// Müstahsile ödeme yapıldığında yayınlanır (docs/02 §3.4 <c>PaymentMade</c>). Ödeme cari
/// alacağı azaltan bir borç hareketi olarak deftere işlenir. Bildirim dinler (docs/02 §6).
/// Kanal ve tutar BK-6 kapsamında (7.000 TL üstü nakit yok). Event adı PascalCase geçmiş zaman.
/// </summary>
public sealed record PaymentMade(
    Guid CurrentAccountId,
    Guid TenantId,
    Guid PartyId,
    decimal Amount,
    DateTime OccurredOnUtc) : IDomainEvent;

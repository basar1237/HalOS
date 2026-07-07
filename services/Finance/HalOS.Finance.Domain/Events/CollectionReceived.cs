using HalOS.BuildingBlocks.Domain;

namespace HalOS.Finance.Domain.Events;

/// <summary>
/// Alıcıdan tahsilat alındığında yayınlanır (docs/02 §3.4 <c>CollectionReceived</c>). Tahsilat
/// alıcının borcunu azaltan bir alacak hareketi olarak deftere işlenir. Bildirim dinler
/// (docs/02 §6). Kanal ve tutar BK-6 kapsamında. Event adı PascalCase geçmiş zaman (docs/07 §3).
/// </summary>
public sealed record CollectionReceived(
    Guid CurrentAccountId,
    Guid TenantId,
    Guid PartyId,
    decimal Amount,
    DateTime OccurredOnUtc) : IDomainEvent;

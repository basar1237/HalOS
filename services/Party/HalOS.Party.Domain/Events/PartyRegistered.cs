using HalOS.BuildingBlocks.Domain;

namespace HalOS.Party.Domain.Events;

/// <summary>Yeni bir taraf (cari kart) kaydedildiğinde yayınlanır (docs/02 §6 katalog deseni).</summary>
public sealed record PartyRegistered(
    Guid PartyId,
    Guid TenantId,
    string DisplayName,
    DateTime OccurredOnUtc) : IDomainEvent;

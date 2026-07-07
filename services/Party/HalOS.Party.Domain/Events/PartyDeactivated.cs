using HalOS.BuildingBlocks.Domain;

namespace HalOS.Party.Domain.Events;

/// <summary>Bir taraf (cari kart) pasifleştirildiğinde yayınlanır.</summary>
public sealed record PartyDeactivated(
    Guid PartyId,
    Guid TenantId,
    DateTime OccurredOnUtc) : IDomainEvent;

using HalOS.BuildingBlocks.Domain;

namespace HalOS.Sales.Domain.Events;

/// <summary>
/// Bir mal geliş partisi kabul edildiğinde yayınlanır (docs/02 §6: Satış → Stok, e-Belge/künye).
/// Event adı PascalCase geçmiş zaman (docs/07 §3).
/// </summary>
public sealed record ConsignmentReceived(
    Guid ConsignmentId,
    Guid TenantId,
    Guid ProducerPartyId,
    DateTime ReceivedAt,
    DateTime OccurredOnUtc) : IDomainEvent;

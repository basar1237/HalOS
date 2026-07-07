using HalOS.BuildingBlocks.Domain;

namespace HalOS.Identity.Domain.Events;

/// <summary>Yeni bir işletme (tenant) oluşturulduğunda yayınlanır.</summary>
public sealed record TenantCreated(Guid TenantId, string Name, DateTime OccurredOnUtc)
    : IDomainEvent;

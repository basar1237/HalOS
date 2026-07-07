using HalOS.BuildingBlocks.Domain;

namespace HalOS.Identity.Domain.Events;

/// <summary>Yeni bir kullanıcı kaydedildiğinde yayınlanır.</summary>
public sealed record UserRegistered(
    Guid UserId,
    Guid TenantId,
    string Email,
    DateTime OccurredOnUtc) : IDomainEvent;

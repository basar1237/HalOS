using HalOS.BuildingBlocks.Domain;

namespace HalOS.Identity.Domain.Events;

/// <summary>Kullanıcı 2FA (TOTP) doğrulamasını tamamlayıp etkinleştirdiğinde yayınlanır.</summary>
public sealed record UserTwoFactorEnabled(
    Guid UserId,
    Guid TenantId,
    DateTime OccurredOnUtc) : IDomainEvent;

namespace HalOS.Identity.Application.Contracts;

/// <summary>Oturum açmış kullanıcının özeti (GET /me).</summary>
public sealed record CurrentUserDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string FullName,
    string Role,
    bool TwoFactorEnabled);

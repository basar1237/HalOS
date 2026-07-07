namespace HalOS.Identity.Application.Contracts;

/// <summary>Başarılı kimlik doğrulama sonucu (token çifti + kullanıcı özeti).</summary>
public sealed record AuthenticationResult(
    string AccessToken,
    DateTime AccessTokenExpiresOnUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresOnUtc,
    Guid UserId,
    Guid TenantId,
    string Email,
    string Role);

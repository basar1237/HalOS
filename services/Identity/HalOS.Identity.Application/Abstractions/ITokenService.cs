using HalOS.Identity.Domain.Aggregates;

namespace HalOS.Identity.Application.Abstractions;

/// <summary>Üretilen bir erişim + refresh token çifti.</summary>
public sealed record TokenPair(
    string AccessToken,
    DateTime AccessTokenExpiresOnUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresOnUtc);

/// <summary>
/// JWT erişim + refresh token üretimi port'u (docs/04 ADR-009). Access token kısa ömürlü;
/// refresh token uzun ömürlü ve rotasyonlu. Ham refresh token yalnızca burada üretilir;
/// depoda hash'i tutulur.
/// </summary>
public interface ITokenService
{
    /// <summary>Kullanıcı için access + refresh token çifti üretir.</summary>
    TokenPair CreateTokenPair(User user);

    /// <summary>Ham refresh token'ı depolamak/karşılaştırmak için hash'ler.</summary>
    string HashRefreshToken(string refreshToken);
}

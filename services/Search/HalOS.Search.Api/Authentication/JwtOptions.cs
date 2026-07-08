namespace HalOS.Search.Api.Authentication;

/// <summary>JWT doğrulama yapılandırması (docs/04 ADR-009). İmza anahtarı üretimde Vault/ortam değişkeninden.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "HalOS.Identity";

    public string Audience { get; set; } = "HalOS";

    /// <summary>HS256 imza anahtarı. Development dışında zorunlu (bkz. <see cref="JwtSigningKeyResolver"/>).</summary>
    public string SigningKey { get; set; } = string.Empty;
}

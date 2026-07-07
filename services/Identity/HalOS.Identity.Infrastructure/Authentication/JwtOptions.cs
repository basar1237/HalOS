namespace HalOS.Identity.Infrastructure.Authentication;

/// <summary>JWT üretim/doğrulama ayarları (appsettings "Jwt" bölümünden).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "HalOS.Identity";

    public string Audience { get; set; } = "HalOS";

    /// <summary>HMAC imzalama anahtarı. Üretimde Vault/Key Vault'tan gelir (docs/07 §güvenlik).</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}

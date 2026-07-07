using System.Security.Cryptography;
using Serilog;

namespace HalOS.Sales.Api.Authentication;

/// <summary>
/// JWT imzalama/doğrulama anahtarını güvenli biçimde çözer (docs/04 §7, docs/07 §güvenlik).
/// Identity/Party servisindeki desenle birebir: Development dışında eksik/zayıf anahtar →
/// fail-fast; Development'ta anahtar yoksa geçici güçlü anahtar üretilir ve log'lanır.
/// </summary>
internal static class JwtSigningKeyResolver
{
    /// <summary>HS256 için asgari anahtar uzunluğu (256 bit = 32 bayt) — RFC 7518.</summary>
    public const int MinimumKeyLengthBytes = 32;

    public static string Resolve(string? configuredKey, bool isDevelopment)
    {
        var key = configuredKey ?? string.Empty;
        var byteLength = System.Text.Encoding.UTF8.GetByteCount(key);

        if (byteLength >= MinimumKeyLengthBytes)
        {
            return key;
        }

        if (!isDevelopment)
        {
            throw new InvalidOperationException(
                $"JWT imzalama anahtarı ('{JwtOptions.SectionName}:SigningKey') eksik veya çok kısa. " +
                $"En az {MinimumKeyLengthBytes} bayt gereklidir. Anahtarı ortam değişkeni/Vault üzerinden sağlayın.");
        }

        var generated = Convert.ToBase64String(RandomNumberGenerator.GetBytes(MinimumKeyLengthBytes));
        Log.Warning(
            "JWT SigningKey yapılandırılmamış; DEVELOPMENT için geçici anahtar üretildi " +
            "(her başlangıçta değişir): {GeneratedKey}",
            generated);

        return generated;
    }
}

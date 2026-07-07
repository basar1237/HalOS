using System.Security.Cryptography;
using Serilog;

namespace HalOS.Identity.Api.Authentication;

/// <summary>
/// JWT imzalama anahtarını güvenli biçimde çözer (docs/04 §7, docs/07 §güvenlik).
///
/// Kural:
/// - Development DIŞINDA anahtar boş veya çok kısa ise başlangıçta <b>fail-fast</b>
///   (<see cref="InvalidOperationException"/>). Zayıf/sabit anahtara asla düşülmez.
/// - Development'ta anahtar yoksa geçici, kriptografik olarak güçlü bir anahtar üretilir ve
///   log'lanır (yalnız yerel geliştirme kolaylığı; üretimde anahtar Vault/ortam değişkeninden).
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
            // Üretim/staging: eksik ya da zayıf anahtar kabul edilemez → hemen çök.
            throw new InvalidOperationException(
                $"JWT imzalama anahtarı ('{Infrastructure.Authentication.JwtOptions.SectionName}:SigningKey') " +
                $"eksik veya çok kısa. En az {MinimumKeyLengthBytes} bayt gereklidir. " +
                "Anahtarı ortam değişkeni/Vault üzerinden sağlayın.");
        }

        // Development: geçici güçlü anahtar üret ve log'la (her başlangıçta değişir).
        var generated = Convert.ToBase64String(RandomNumberGenerator.GetBytes(MinimumKeyLengthBytes));
        Log.Warning(
            "JWT SigningKey yapılandırılmamış; DEVELOPMENT için geçici anahtar üretildi " +
            "(her başlangıçta değişir, token'lar kalıcı değildir): {GeneratedKey}",
            generated);

        return generated;
    }
}

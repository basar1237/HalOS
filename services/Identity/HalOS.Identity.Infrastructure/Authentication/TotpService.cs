using System.Security.Cryptography;
using HalOS.Identity.Application.Abstractions;
using OtpNet;

namespace HalOS.Identity.Infrastructure.Authentication;

/// <summary>TOTP tabanlı 2FA (docs/04 ADR-009). Otp.NET ile RFC 6238 uyumlu.</summary>
internal sealed class TotpService : ITotpService
{
    // Doğrulamada saat kaymasına karşı ±1 pencere toleransı.
    private static readonly VerificationWindow Window = new(previous: 1, future: 1);

    public string GenerateSecret()
    {
        var key = RandomNumberGenerator.GetBytes(20); // 160-bit
        return Base32Encoding.ToString(key);
    }

    public string BuildOtpAuthUri(string secret, string accountName, string issuer)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedAccount = Uri.EscapeDataString(accountName);
        return $"otpauth://totp/{encodedIssuer}:{encodedAccount}" +
               $"?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code.Trim(), out _, Window);
    }
}

namespace HalOS.Identity.Application.Abstractions;

/// <summary>TOTP tabanlı 2FA port'u (docs/04 ADR-009, docs/07 güvenlik).</summary>
public interface ITotpService
{
    /// <summary>Yeni bir base32 paylaşılan gizli anahtar üretir.</summary>
    string GenerateSecret();

    /// <summary>Authenticator uygulamasında QR kod olarak gösterilecek otpauth URI'si üretir.</summary>
    string BuildOtpAuthUri(string secret, string accountName, string issuer);

    /// <summary>Verilen gizli anahtar için TOTP kodunu doğrular.</summary>
    bool VerifyCode(string secret, string code);
}

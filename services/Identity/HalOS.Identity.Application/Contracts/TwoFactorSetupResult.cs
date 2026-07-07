namespace HalOS.Identity.Application.Contracts;

/// <summary>2FA kurulumu başlatıldığında dönen bilgiler (gizli anahtar + QR URI).</summary>
public sealed record TwoFactorSetupResult(string Secret, string OtpAuthUri);

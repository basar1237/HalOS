using HalOS.BuildingBlocks.Application;
using HalOS.Identity.Application.Contracts;

namespace HalOS.Identity.Application.Features.Users.Setup2fa;

/// <summary>
/// O anki kullanıcı için 2FA (TOTP) kurulumunu başlatır: gizli anahtar üretir ve saklar,
/// authenticator uygulaması için otpauth URI döner. Etkinleştirme, kod doğrulanınca yapılır.
/// </summary>
public sealed record Setup2faCommand : ICommand<TwoFactorSetupResult>;

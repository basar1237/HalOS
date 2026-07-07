using HalOS.BuildingBlocks.Application;
using HalOS.Identity.Application.Contracts;

namespace HalOS.Identity.Application.Features.Users.Login;

/// <summary>
/// Kullanıcıyı e-posta + parola ile doğrular. Kullanıcının 2FA'sı etkinse
/// <see cref="TwoFactorCode"/> zorunludur.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password,
    string? TwoFactorCode) : ICommand<AuthenticationResult>;

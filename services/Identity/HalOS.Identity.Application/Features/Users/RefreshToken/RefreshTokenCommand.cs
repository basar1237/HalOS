using HalOS.BuildingBlocks.Application;
using HalOS.Identity.Application.Contracts;

namespace HalOS.Identity.Application.Features.Users.RefreshToken;

/// <summary>Geçerli bir refresh token'ı yeni bir token çifti ile takas eder (rotasyon).</summary>
public sealed record RefreshTokenCommand(string RefreshToken)
    : ICommand<AuthenticationResult>;

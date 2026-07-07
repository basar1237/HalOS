using HalOS.Identity.Domain.Enums;

namespace HalOS.Identity.Api.Contracts;

public sealed record RegisterRequest(
    Guid TenantId,
    string Email,
    string Password,
    string FullName,
    SystemRole Role);

public sealed record LoginRequest(string Email, string Password, string? TwoFactorCode);

public sealed record RefreshRequest(string RefreshToken);

public sealed record CreateTenantRequest(string Name);

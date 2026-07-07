using HalOS.BuildingBlocks.Application;
using HalOS.Identity.Domain.Enums;

namespace HalOS.Identity.Application.Features.Users.RegisterUser;

/// <summary>
/// Belirtilen tenant'a yeni bir kullanıcı kaydeder. <see cref="TenantId"/> genelde
/// tenant çözümleme middleware'inden (JWT claim) gelir; ilk kurulum için doğrudan verilebilir.
/// </summary>
public sealed record RegisterUserCommand(
    Guid TenantId,
    string Email,
    string Password,
    string FullName,
    SystemRole Role) : ICommand<Guid>;

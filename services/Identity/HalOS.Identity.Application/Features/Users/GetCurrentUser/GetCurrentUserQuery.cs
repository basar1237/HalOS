using HalOS.BuildingBlocks.Application;
using HalOS.Identity.Application.Contracts;

namespace HalOS.Identity.Application.Features.Users.GetCurrentUser;

/// <summary>O anki oturum açmış kullanıcının özetini getirir (GET /me).</summary>
public sealed record GetCurrentUserQuery : IQuery<CurrentUserDto>;

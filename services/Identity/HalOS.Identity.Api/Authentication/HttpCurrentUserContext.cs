using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HalOS.Identity.Application.Abstractions;

namespace HalOS.Identity.Api.Authentication;

/// <summary>İsteğin kimliği doğrulanmış kullanıcısını JWT "sub" claim'inden çözer.</summary>
internal sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _accessor.HttpContext?.User;
            var value = user?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated =>
        _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

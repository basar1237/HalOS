using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HalOS.BuildingBlocks.Application;

namespace HalOS.Party.Api.Authentication;

/// <summary>
/// İsteğin kullanıcısını ("kim") JWT subject (sub / NameIdentifier) claim'inden çözer; denetim
/// (audit_log) yazımı için (docs/05 §3.11, docs/03 §6). Party servisinde ayrı bir
/// <c>ICurrentUserContext</c> bulunmadığından <see cref="IAuditActor"/> doğrudan claim'den okunur
/// (diğer servislerdeki HttpCurrentUserContext deseniyle paralel). Anonim isteklerde
/// <see cref="HasUser"/> false, <see cref="UserId"/> <see cref="Guid.Empty"/> döner.
/// </summary>
internal sealed class HttpAuditActor : IAuditActor
{
    private readonly IHttpContextAccessor _accessor;

    public HttpAuditActor(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid UserId
    {
        get
        {
            var user = _accessor.HttpContext?.User;
            var value = user?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
        }
    }

    public bool HasUser => UserId != Guid.Empty;
}

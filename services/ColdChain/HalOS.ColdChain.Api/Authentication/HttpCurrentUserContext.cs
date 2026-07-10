using System.Security.Claims;
using HalOS.ColdChain.Application.Abstractions;

namespace HalOS.ColdChain.Api.Authentication;

/// <summary>
/// İsteğin kullanıcısını JWT subject (sub / NameIdentifier) claim'inden çözer; stok/fire kayıtlarının
/// denetim alanları için kullanılır (docs/05 §1, docs/03 §6). Anonim isteklerde
/// <see cref="Guid.Empty"/> döner. Finance deseniyle birebir.
/// </summary>
internal sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid UserId
    {
        get
        {
            var user = _accessor.HttpContext?.User;
            var value = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user?.FindFirstValue("sub");
            return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
        }
    }
}

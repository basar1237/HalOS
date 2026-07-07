using System.Security.Claims;
using HalOS.Sales.Application.Abstractions;

namespace HalOS.Sales.Api.Authentication;

/// <summary>
/// İsteğin kullanıcısını JWT subject (sub / NameIdentifier) claim'inden çözer; mali kayıtların
/// <c>created_by</c> denetim alanı için kullanılır (docs/05 §1, docs/03 §6). Anonim isteklerde
/// <see cref="Guid.Empty"/> döner.
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

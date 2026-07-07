using System.Security.Claims;
using HalOS.BuildingBlocks.Application;

namespace HalOS.Party.Api.Authentication;

/// <summary>
/// İsteğin tenant'ını JWT tenant claim'inden çözer (docs/04 §7, docs/07 §6). EF Core global
/// query filter bu değeri kullanır. Anonim isteklerde tenant çözülmez (HasTenant = false).
/// </summary>
internal sealed class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpTenantContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid TenantId
    {
        get
        {
            var value = _accessor.HttpContext?.User?.FindFirstValue(HalOSClaimTypes.TenantId);
            return Guid.TryParse(value, out var tenantId) ? tenantId : Guid.Empty;
        }
    }

    public bool HasTenant
    {
        get
        {
            var value = _accessor.HttpContext?.User?.FindFirstValue(HalOSClaimTypes.TenantId);
            return Guid.TryParse(value, out var tenantId) && tenantId != Guid.Empty;
        }
    }
}

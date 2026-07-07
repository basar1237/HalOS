namespace HalOS.BuildingBlocks.Application;

/// <summary>
/// Provides the current request's tenant. Populated from the JWT tenant claim and carried
/// through the request context (docs/04 §7, docs/07 §6). Infrastructure uses it to apply
/// the mandatory <c>tenant_id</c> global query filter — repositories never query unfiltered.
/// </summary>
public interface ITenantContext
{
    /// <summary>The current tenant's identifier.</summary>
    Guid TenantId { get; }

    /// <summary>
    /// Whether a tenant is resolved for the current context. False for system/background
    /// contexts that legitimately run without a tenant (e.g. some maintenance jobs).
    /// </summary>
    bool HasTenant { get; }
}

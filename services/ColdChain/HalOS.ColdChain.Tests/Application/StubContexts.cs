using HalOS.BuildingBlocks.Application;

namespace HalOS.ColdChain.Tests.Application;

/// <summary>Handler unit testleri için tenant stub bağlamı.</summary>
internal sealed class StubTenantContext : ITenantContext
{
    public StubTenantContext(Guid tenantId) => TenantId = tenantId;
    public Guid TenantId { get; }
    public bool HasTenant => TenantId != Guid.Empty;
}

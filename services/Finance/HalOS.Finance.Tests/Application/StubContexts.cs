using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Application.Abstractions;

namespace HalOS.Finance.Tests.Application;

/// <summary>Handler unit testleri için tenant/kullanıcı stub bağlamları (Sales deseniyle birebir).</summary>
internal sealed class StubTenantContext : ITenantContext
{
    public StubTenantContext(Guid tenantId) => TenantId = tenantId;
    public Guid TenantId { get; }
    public bool HasTenant => TenantId != Guid.Empty;
}

internal sealed class StubCurrentUserContext : ICurrentUserContext
{
    public StubCurrentUserContext(Guid userId) => UserId = userId;
    public Guid UserId { get; }
}

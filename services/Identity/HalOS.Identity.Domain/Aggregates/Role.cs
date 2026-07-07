using HalOS.BuildingBlocks.Domain;
using HalOS.Identity.Domain.Enums;

namespace HalOS.Identity.Domain.Aggregates;

/// <summary>
/// Rol aggregate'i (docs/02 §1 <c>Role</c>). Tenant'a ait; RBAC yetkilendirmesinin
/// temeli (docs/03 §3). Öntanımlı sistem rolleri her tenant için seed edilir.
/// </summary>
public sealed class Role : AggregateRoot<Guid>, ITenantOwned
{
    private Role(Guid id, Guid tenantId, SystemRole systemRole, string name)
        : base(id)
    {
        TenantId = tenantId;
        SystemRole = systemRole;
        Name = name;
    }

    private Role()
    {
        Name = string.Empty;
    }

    public Guid TenantId { get; private set; }

    /// <summary>Bu rolün karşılık geldiği öntanımlı sistem rolü.</summary>
    public SystemRole SystemRole { get; private set; }

    /// <summary>Rolün kod adı (RBAC politikası bu ada bağlanır), örn. "Owner".</summary>
    public string Name { get; private set; }

    public static Role Create(Guid tenantId, SystemRole systemRole)
    {
        return new Role(Guid.NewGuid(), tenantId, systemRole, systemRole.ToString());
    }
}

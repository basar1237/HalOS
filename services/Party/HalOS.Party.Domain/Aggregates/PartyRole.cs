using HalOS.BuildingBlocks.Domain;
using HalOS.Party.Domain.Enums;

namespace HalOS.Party.Domain.Aggregates;

/// <summary>
/// Bir <see cref="Party"/>'nin taşıdığı tek bir rol (docs/02 §1.1, §3.1). Party aggregate'inin
/// bağlı entity'sidir; yaşam döngüsü kök tarafından yönetilir. Bir taraf birden çok rol
/// taşıyabildiğinden (Producer/Buyer/Merchant/Consignor) ayrı bir <c>party_role</c> tablosunda
/// tutulur (docs/05 §3.2). ITenantOwned'dır: global query filter role tablosuna da uygulanır.
/// </summary>
public sealed class PartyRole : Entity<Guid>, ITenantOwned
{
    private PartyRole(Guid id, Guid partyId, Guid tenantId, PartyRoleType type)
        : base(id)
    {
        PartyId = partyId;
        TenantId = tenantId;
        Type = type;
    }

    /// <summary>ORM materialization only.</summary>
    private PartyRole()
    {
    }

    public Guid PartyId { get; private set; }

    public Guid TenantId { get; private set; }

    public PartyRoleType Type { get; private set; }

    internal static PartyRole Create(Guid partyId, Guid tenantId, PartyRoleType type) =>
        new(Guid.NewGuid(), partyId, tenantId, type);
}

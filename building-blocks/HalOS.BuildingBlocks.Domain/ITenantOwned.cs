namespace HalOS.BuildingBlocks.Domain;

/// <summary>
/// Marks a business entity as owned by a tenant. Every business entity must carry a
/// <c>TenantId</c> (docs/07 §6); implementing this interface lets the infrastructure apply
/// the mandatory EF Core global query filter automatically (docs/04 ADR-008, docs/05).
/// </summary>
public interface ITenantOwned
{
    /// <summary>Owning tenant identifier (maps to the <c>tenant_id</c> column).</summary>
    Guid TenantId { get; }
}

using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Domain.Enums;

namespace HalOS.Sales.Domain.Aggregates;

/// <summary>
/// Bir <see cref="Consignment"/> içindeki tek gelen kalem (docs/02 §3.2; docs/05 §3.4
/// <c>consignment_item</c>). Consignment aggregate'inin bağlı entity'sidir; yaşam döngüsü kök
/// tarafından yönetilir. Ürün referansı ID ile tutulur (servisler arası FK yok — docs/05 §5).
/// Miktar NUMERIC(18,3) (docs/05 §3.4; görevde decimal(3) = 3 ondalık).
/// </summary>
public sealed class ConsignmentItem : Entity<Guid>, ITenantOwned
{
    private ConsignmentItem(
        Guid id,
        Guid consignmentId,
        Guid tenantId,
        Guid productId,
        decimal quantity,
        UnitOfMeasure unit)
        : base(id)
    {
        ConsignmentId = consignmentId;
        TenantId = tenantId;
        ProductId = productId;
        Quantity = quantity;
        Unit = unit;
    }

    /// <summary>ORM materialization only.</summary>
    private ConsignmentItem()
    {
    }

    public Guid ConsignmentId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Ürün referansı (Inventory servisi ID'si — FK değil, docs/05 §5).</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Gelen miktar (NUMERIC(18,3)).</summary>
    public decimal Quantity { get; private set; }

    public UnitOfMeasure Unit { get; private set; }

    internal static ConsignmentItem Create(
        Guid consignmentId,
        Guid tenantId,
        Guid productId,
        decimal quantity,
        UnitOfMeasure unit) =>
        new(Guid.NewGuid(), consignmentId, tenantId, productId, quantity, unit);
}

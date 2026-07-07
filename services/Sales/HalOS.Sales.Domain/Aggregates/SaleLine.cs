using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Domain.Enums;
using HalOS.Sales.Domain.ValueObjects;

namespace HalOS.Sales.Domain.Aggregates;

/// <summary>
/// Satış satırı (docs/02 §1.4, §3.3; docs/05 §3.5 <c>sale_line</c>). Bir <see cref="SaleTransaction"/>'ın
/// bağlı entity'sidir. Ürün referansı ID ile (servisler arası FK yok — docs/05 §5).
///
/// Para değişmezi (docs/02 §3.3, BK-1/BK-2): <c>LineAmount = Quantity * UnitPrice</c>, decimal;
/// satır tutarı kuruşa yuvarlanır (<see cref="Money.RoundToKurus"/>). Miktar NUMERIC(18,3),
/// birim fiyat/tutar NUMERIC(18,2) (docs/05 §3.5).
/// </summary>
public sealed class SaleLine : Entity<Guid>, ITenantOwned
{
    private SaleLine(
        Guid id,
        Guid saleTransactionId,
        Guid tenantId,
        Guid productId,
        decimal quantity,
        UnitOfMeasure unit,
        decimal unitPrice)
        : base(id)
    {
        SaleTransactionId = saleTransactionId;
        TenantId = tenantId;
        ProductId = productId;
        Quantity = quantity;
        Unit = unit;
        UnitPrice = unitPrice;
        LineAmount = Money.RoundToKurus(quantity * unitPrice);
    }

    /// <summary>ORM materialization only.</summary>
    private SaleLine()
    {
    }

    public Guid SaleTransactionId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Ürün referansı (Inventory servisi ID'si — FK değil, docs/05 §5).</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Miktar (NUMERIC(18,3)).</summary>
    public decimal Quantity { get; private set; }

    public UnitOfMeasure Unit { get; private set; }

    /// <summary>Birim fiyat (NUMERIC(18,2); decimal — asla float, BK-2).</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Satır tutarı = miktar × birim fiyat, kuruşa yuvarlı (NUMERIC(18,2), BK-1/BK-2).</summary>
    public decimal LineAmount { get; private set; }

    internal static SaleLine Create(
        Guid saleTransactionId,
        Guid tenantId,
        Guid productId,
        decimal quantity,
        UnitOfMeasure unit,
        decimal unitPrice) =>
        new(Guid.NewGuid(), saleTransactionId, tenantId, productId, quantity, unit, unitPrice);
}

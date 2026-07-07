using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Domain.Enums;

namespace HalOS.Sales.Domain.Aggregates;

/// <summary>
/// Tek bir kesinti kalemi (docs/02 §3.3; docs/05 §3.5 <c>deduction</c>). Komisyon, zirai stopaj,
/// çiftçi Bağ-Kur, hal rüsumu ve komisyon KDV'si AYRI satırlar olarak tutulur — yasal olarak
/// birleştirilmez (docs/02 §7 anti-pattern). Bir <see cref="SaleTransaction"/>'ın bağlı
/// entity'sidir. Oran NUMERIC(7,4), tutar NUMERIC(18,2) (docs/05 §1; decimal — BK-2).
/// </summary>
public sealed class Deduction : Entity<Guid>, ITenantOwned
{
    private Deduction(
        Guid id,
        Guid saleTransactionId,
        Guid tenantId,
        DeductionType type,
        decimal rate,
        decimal amount)
        : base(id)
    {
        SaleTransactionId = saleTransactionId;
        TenantId = tenantId;
        Type = type;
        Rate = rate;
        Amount = amount;
    }

    /// <summary>ORM materialization only.</summary>
    private Deduction()
    {
    }

    public Guid SaleTransactionId { get; private set; }

    public Guid TenantId { get; private set; }

    public DeductionType Type { get; private set; }

    /// <summary>Uygulanan oran (NUMERIC(7,4)).</summary>
    public decimal Rate { get; private set; }

    /// <summary>Kesinti tutarı — kuruşa yuvarlı (NUMERIC(18,2), BK-2).</summary>
    public decimal Amount { get; private set; }

    internal static Deduction Create(
        Guid saleTransactionId,
        Guid tenantId,
        DeductionType type,
        decimal rate,
        decimal amount) =>
        new(Guid.NewGuid(), saleTransactionId, tenantId, type, rate, amount);
}

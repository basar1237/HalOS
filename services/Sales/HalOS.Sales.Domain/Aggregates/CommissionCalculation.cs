using HalOS.BuildingBlocks.Domain;

namespace HalOS.Sales.Domain.Aggregates;

/// <summary>
/// Komisyon hesabı (docs/02 §3.3; docs/05 §3.5 <c>commission_calculation</c>, satışla 1:1).
/// Komisyon tutarını ve komisyon üzerine HESAPLANAN KDV'yi tutar. KDV komisyoncunun gelir
/// vergisidir; müstahsil hakedişinden DÜŞÜLMEZ (docs/02 §4, BK-1). Bir <see cref="SaleTransaction"/>'ın
/// bağlı entity'sidir. Oran NUMERIC(7,4), tutar NUMERIC(18,2) (decimal — BK-2).
/// </summary>
public sealed class CommissionCalculation : Entity<Guid>, ITenantOwned
{
    private CommissionCalculation(
        Guid id,
        Guid saleTransactionId,
        Guid tenantId,
        decimal commissionRate,
        decimal commissionAmount,
        decimal vatRate,
        decimal vatAmount)
        : base(id)
    {
        SaleTransactionId = saleTransactionId;
        TenantId = tenantId;
        CommissionRate = commissionRate;
        CommissionAmount = commissionAmount;
        VatRate = vatRate;
        VatAmount = vatAmount;
    }

    /// <summary>ORM materialization only.</summary>
    private CommissionCalculation()
    {
    }

    public Guid SaleTransactionId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Uygulanan komisyon oranı (NUMERIC(7,4); ≤ %8, BK-1).</summary>
    public decimal CommissionRate { get; private set; }

    /// <summary>Komisyon tutarı = Round(gross × commissionRate, 2, ToEven) (BK-1/BK-2).</summary>
    public decimal CommissionAmount { get; private set; }

    /// <summary>Komisyon KDV oranı (NUMERIC(7,4)).</summary>
    public decimal VatRate { get; private set; }

    /// <summary>Komisyon KDV tutarı = Round(commission × vatRate, 2) — hakedişten düşülmez (BK-1).</summary>
    public decimal VatAmount { get; private set; }

    internal static CommissionCalculation Create(
        Guid saleTransactionId,
        Guid tenantId,
        decimal commissionRate,
        decimal commissionAmount,
        decimal vatRate,
        decimal vatAmount) =>
        new(Guid.NewGuid(), saleTransactionId, tenantId, commissionRate, commissionAmount, vatRate, vatAmount);
}

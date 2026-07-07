using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Domain.Enums;

namespace HalOS.Sales.Domain.Aggregates;

/// <summary>
/// Müstahsile hakediş (docs/02 §1.3, §3.3; docs/05 §3.5 <c>settlement</c>, satışla 1:1).
/// Kesintiler sonrası ödenecek net tutarı ve ödeme vade tarihini tutar.
///
/// Değişmezler (docs/02 §3.3, docs/03 §4 BK-1/BK-3):
/// - <see cref="NetAmount"/> = brüt − (komisyon + stopaj + Bağ-Kur + rüsum). ASLA negatif olamaz.
/// - <see cref="DueDate"/> = satış tarihi + 15 İŞ GÜNÜ (hafta sonu atlanır; resmi tatil kapsam
///   dışı — bkz. <c>SaleTransaction.Complete</c> notu).
/// Bir <see cref="SaleTransaction"/>'ın bağlı entity'sidir. Tutar NUMERIC(18,2) (decimal — BK-2).
/// </summary>
public sealed class Settlement : Entity<Guid>, ITenantOwned
{
    private Settlement(
        Guid id,
        Guid saleTransactionId,
        Guid tenantId,
        decimal netAmount,
        DateTime dueDate)
        : base(id)
    {
        SaleTransactionId = saleTransactionId;
        TenantId = tenantId;
        NetAmount = netAmount;
        DueDate = dueDate;
        Status = SettlementStatus.Pending;
    }

    /// <summary>ORM materialization only.</summary>
    private Settlement()
    {
    }

    public Guid SaleTransactionId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Müstahsile net hakediş (NUMERIC(18,2)). Değişmez: negatif olamaz (BK-1).</summary>
    public decimal NetAmount { get; private set; }

    /// <summary>Ödeme vade tarihi = satış + 15 iş günü (BK-3).</summary>
    public DateTime DueDate { get; private set; }

    public SettlementStatus Status { get; private set; }

    /// <summary>
    /// Hakedişi oluşturur. Net tutar negatifse başarısız döner — hakediş asla negatif olamaz
    /// (docs/02 §3.3 değişmez, BK-1).
    /// </summary>
    internal static Result<Settlement> Create(
        Guid saleTransactionId,
        Guid tenantId,
        decimal netAmount,
        DateTime dueDate)
    {
        if (netAmount < 0m)
        {
            return Result.Failure<Settlement>(SettlementErrors.NegativeNet);
        }

        return new Settlement(Guid.NewGuid(), saleTransactionId, tenantId, netAmount, dueDate);
    }
}

public static class SettlementErrors
{
    public static readonly Error NegativeNet =
        new("Settlement.NegativeNet", "Müstahsil hakedişi (net) negatif olamaz.");
}

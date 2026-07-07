using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Domain.Enums;

namespace HalOS.Integration.Domain.Aggregates;

/// <summary>
/// e-Müstahsil Makbuzu (e-MM) üzerindeki tek bir kesinti kalemi (docs/02 §1.3 / §3.5). e-MM YALNIZ
/// müstahsilden kesilen stopaj + çiftçi Bağ-Kur kalemlerini içerir (komisyon/rüsum/KDV e-MM'e girmez
/// — docs/02 §1.2, BK-1/BK-4). Bir <see cref="ProducerReceipt"/>'in bağlı entity'sidir. Tutar
/// NUMERIC(18,2) (decimal — BK-2). Kalemler yasal olarak AYRI tutulur (tek "fee" altında birleştirilmez
/// — docs/02 §7 anti-pattern).
/// </summary>
public sealed class ReceiptDeduction : Entity<Guid>, ITenantOwned
{
    private ReceiptDeduction(
        Guid id,
        Guid producerReceiptId,
        Guid tenantId,
        ReceiptDeductionType type,
        decimal amount)
        : base(id)
    {
        ProducerReceiptId = producerReceiptId;
        TenantId = tenantId;
        Type = type;
        Amount = amount;
    }

    /// <summary>ORM materialization only.</summary>
    private ReceiptDeduction()
    {
    }

    public Guid ProducerReceiptId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Kesinti türü (stopaj / çiftçi Bağ-Kur).</summary>
    public ReceiptDeductionType Type { get; private set; }

    /// <summary>Kesinti tutarı — kuruşa yuvarlı, negatif olamaz (NUMERIC(18,2), BK-2).</summary>
    public decimal Amount { get; private set; }

    /// <summary>Yeni bir e-MM kesinti kalemi üretir (aggregate içinden çağrılır).</summary>
    internal static ReceiptDeduction Create(
        Guid producerReceiptId,
        Guid tenantId,
        ReceiptDeductionType type,
        decimal amount) =>
        new(Guid.NewGuid(), producerReceiptId, tenantId, type, amount);
}

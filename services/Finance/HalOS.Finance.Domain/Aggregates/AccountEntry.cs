using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Domain.Enums;

namespace HalOS.Finance.Domain.Aggregates;

/// <summary>
/// Tek bir cari hareket (docs/02 §3.4; docs/05 §3.7 <c>account_entry</c>). APPEND-ONLY: hareketler
/// silinmez/değiştirilmez, düzeltme ters kayıtla yapılır (docs/05 §4 değişmez; mali tabloya
/// destructive işlem yasak — docs/07 §8). Bakiye bu hareketlerden türetilir; <see cref="Direction"/>
/// borç/alacak yönünü, <see cref="Type"/> ise iş olayı türünü taşır. Bir <see cref="CurrentAccount"/>'ın
/// bağlı entity'sidir. Tutar NUMERIC(18,2) (decimal — BK-2); vade tarihi (opsiyonel) yalnız hakediş
/// (Settlement) hareketlerinde dolar (15 iş günü — BK-3).
/// </summary>
public sealed class AccountEntry : Entity<Guid>, ITenantOwned
{
    private AccountEntry(
        Guid id,
        Guid currentAccountId,
        Guid tenantId,
        EntryDirection direction,
        EntryType type,
        decimal amount,
        Guid? refId,
        DateTime occurredAt,
        DateTime? dueDate)
        : base(id)
    {
        CurrentAccountId = currentAccountId;
        TenantId = tenantId;
        Direction = direction;
        Type = type;
        Amount = amount;
        RefId = refId;
        OccurredAt = occurredAt;
        DueDate = dueDate;
    }

    /// <summary>ORM materialization only.</summary>
    private AccountEntry()
    {
    }

    public Guid CurrentAccountId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Borç (debit) / alacak (credit) yönü — bakiye türetimini belirler.</summary>
    public EntryDirection Direction { get; private set; }

    /// <summary>Hareketin iş olayı türü (sale/settlement/payment/collection/advance/adjustment).</summary>
    public EntryType Type { get; private set; }

    /// <summary>Hareket tutarı — kuruşa yuvarlı, pozitif (NUMERIC(18,2), BK-2).</summary>
    public decimal Amount { get; private set; }

    /// <summary>İlgili satış/ödeme/tahsilat referansı (docs/05 §3.7 <c>ref_id</c>; FK değil — docs/05 §5).</summary>
    public Guid? RefId { get; private set; }

    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// Ödeme vade tarihi — yalnızca hakediş (<see cref="EntryType.Settlement"/>) hareketlerinde
    /// dolar (satış + 15 iş günü — BK-3). Diğer hareketlerde null.
    /// </summary>
    public DateTime? DueDate { get; private set; }

    /// <summary>
    /// Yeni bir cari hareket üretir (aggregate içinden çağrılır). Tutar pozitif ve kuruşa yuvarlı
    /// olmalıdır; sıfır/negatif hareket kaydı değişmezi ihlal eder (bkz. <see cref="CurrentAccount"/>).
    /// </summary>
    internal static AccountEntry Create(
        Guid currentAccountId,
        Guid tenantId,
        EntryDirection direction,
        EntryType type,
        decimal amount,
        Guid? refId,
        DateTime occurredAt,
        DateTime? dueDate = null) =>
        new(Guid.NewGuid(), currentAccountId, tenantId, direction, type, amount, refId, occurredAt, dueDate);

    /// <summary>Bu hareketin bakiyeye katkısı: borç (+), alacak (−). Bakiye = Σ katkı.</summary>
    public decimal SignedAmount => Direction == EntryDirection.Debit ? Amount : -Amount;
}

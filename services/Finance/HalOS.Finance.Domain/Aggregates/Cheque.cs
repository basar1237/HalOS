using HalOS.BuildingBlocks.Domain;

namespace HalOS.Finance.Domain.Aggregates;

/// <summary>Çek mi senet mi.</summary>
public enum ChequeKind
{
    Cheque = 1,          // Çek
    PromissoryNote = 2,  // Senet
}

/// <summary>Alınan (müşteriden) / Verilen (tedarikçiye).</summary>
public enum ChequeDirection
{
    Received = 1, // Alınan (portföyde alacak)
    Issued = 2,   // Verilen (borç)
}

/// <summary>Çek/senet yaşam döngüsü durumu.</summary>
public enum ChequeStatus
{
    Portfolio = 1,   // Portföyde
    AtBank = 2,      // Tahsile verildi
    Collected = 3,   // Tahsil edildi
    Bounced = 4,     // Karşılıksız
    Endorsed = 5,    // Ciro edildi
    Paid = 6,        // (verilen çek) ödendi
}

/// <summary>
/// Çek/Senet portföy kaydı (docs/11 §3.5). Alınan/verilen çek ve senetleri; banka, seri no, vade,
/// tutar ve durum ile izler. Tenant'a bağlıdır (ITenantOwned → global query filter, BK-8). Taraf
/// referansı ID ile (FK değil — docs/05 §5). Para decimal, kuruşa yuvarlı (BK-2).
/// </summary>
public sealed class Cheque : AggregateRoot<Guid>, ITenantOwned
{
    private Cheque(
        Guid id,
        Guid tenantId,
        ChequeKind kind,
        ChequeDirection direction,
        Guid? partyId,
        string bankName,
        string serialNo,
        decimal amount,
        DateTime issueDate,
        DateTime dueDate,
        string? note,
        DateTime createdOnUtc)
        : base(id)
    {
        TenantId = tenantId;
        Kind = kind;
        Direction = direction;
        PartyId = partyId;
        BankName = bankName;
        SerialNo = serialNo;
        Amount = amount;
        IssueDate = issueDate;
        DueDate = dueDate;
        Note = note;
        Status = ChequeStatus.Portfolio;
        CreatedOnUtc = createdOnUtc;
    }

    private Cheque() { } // ORM

    public Guid TenantId { get; private set; }
    public ChequeKind Kind { get; private set; }
    public ChequeDirection Direction { get; private set; }
    public Guid? PartyId { get; private set; }
    public string BankName { get; private set; } = string.Empty;
    public string SerialNo { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime IssueDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public ChequeStatus Status { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public static Result<Cheque> Create(
        Guid tenantId,
        ChequeKind kind,
        ChequeDirection direction,
        Guid? partyId,
        string? bankName,
        string? serialNo,
        decimal amount,
        DateTime issueDate,
        DateTime dueDate,
        string? note)
    {
        if (amount <= 0m)
        {
            return Result.Failure<Cheque>(ChequeErrors.InvalidAmount);
        }

        return new Cheque(
            Guid.NewGuid(),
            tenantId,
            kind,
            direction,
            partyId,
            (bankName ?? string.Empty).Trim(),
            (serialNo ?? string.Empty).Trim(),
            Math.Round(amount, 2, MidpointRounding.ToEven),
            issueDate,
            dueDate,
            string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            DateTime.UtcNow);
    }

    /// <summary>Durumu değiştirir (portföy → tahsile ver / tahsil / karşılıksız / ciro / ödendi).</summary>
    public Result ChangeStatus(ChequeStatus newStatus)
    {
        if (Status is ChequeStatus.Collected or ChequeStatus.Endorsed or ChequeStatus.Paid)
        {
            return Result.Failure(ChequeErrors.Finalized);
        }

        Status = newStatus;
        return Result.Success();
    }
}

public static class ChequeErrors
{
    public static readonly Error InvalidAmount =
        new("Cheque.InvalidAmount", "Çek/senet tutarı sıfırdan büyük olmalıdır.");

    public static readonly Error Finalized =
        new("Cheque.Finalized", "Tahsil/ciro/ödenmiş çek-senedin durumu değiştirilemez.");

    public static readonly Error NotFound =
        new("Cheque.NotFound", "Çek/senet kaydı bulunamadı.");
}

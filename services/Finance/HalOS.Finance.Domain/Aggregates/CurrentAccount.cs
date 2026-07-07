using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Domain.Enums;
using HalOS.Finance.Domain.Events;
using HalOS.Finance.Domain.ValueObjects;

namespace HalOS.Finance.Domain.Aggregates;

/// <summary>
/// Cari Hesap (CurrentAccount) — Cari &amp; Finans bağlamının kök aggregate'i (docs/02 §3.4;
/// docs/05 §3.7). Bir <see cref="PartyId"/>'ye (müstahsil/alıcı) bağlı borç-alacak defterini
/// tutar. Tenant'a bağlıdır (ITenantOwned → global query filter, BK-8). Party referansı ID ile
/// (servisler arası FK yok — docs/05 §5).
///
/// Değişmezler (docs/02 §3.4; docs/05 §4):
/// - <c>Balance = Σ AccountEntry.SignedAmount</c> (borç +, alacak −). Hareketler APPEND-ONLY;
///   düzeltme ters kayıtla yapılır (mali tabloya destructive işlem yasak — docs/07 §8).
/// - <b>Yön kuralı</b> (docs/02 §5 operasyonel akış): alıcı carisine satış BORÇ (alıcı öder);
///   müstahsil carisine net hakediş ALACAK (komisyoncu öder). Böylece pozitif bakiye = tarafın
///   işletmeye borcu, negatif bakiye = işletmenin tarafa borcu.
/// - Müstahsile ödeme vade tarihi hakediş hareketinde saklanır (normal satış 15 iş günü — BK-3);
///   vade tarihi yukarı katmandan (SaleCompleted.SettlementDueDate) gelir.
/// - Nakit ödeme/tahsilat 7.000 TL'yi aşamaz (BK-6); avans için de aynı kanal kısıtı geçerlidir.
///
/// Idempotency: aynı satış (SaleTransactionId) yalnız bir kez cariye işlenir; consumer tekrar
/// tetiklenirse çift kayıt oluşmaz (docs/04 §5/§10 en-az-bir-kez teslimat).
/// </summary>
public sealed class CurrentAccount : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<AccountEntry> _entries = new();

    private CurrentAccount(Guid id, Guid tenantId, Guid partyId)
        : base(id)
    {
        TenantId = tenantId;
        PartyId = partyId;
    }

    /// <summary>ORM materialization only.</summary>
    private CurrentAccount()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>Cari sahibi taraf (Party ID — müstahsil/alıcı; FK değil, docs/05 §5).</summary>
    public Guid PartyId { get; private set; }

    public IReadOnlyCollection<AccountEntry> Entries => _entries.AsReadOnly();

    /// <summary>
    /// Bakiye = Σ hareket (borç +, alacak −), kuruşa normalize (docs/02 §3.4 değişmez). Türetilmiş
    /// değer; kalıcılaştırma için okuma modeli/cache olarak da tutulur (docs/05 §3.7 <c>balance</c>).
    /// </summary>
    public decimal Balance => Money.RoundToKurus(_entries.Sum(e => e.SignedAmount));

    /// <summary>Yeni (boş) bir cari hesap açar. Party referansı zorunlu.</summary>
    public static Result<CurrentAccount> Open(Guid tenantId, Guid partyId)
    {
        if (partyId == Guid.Empty)
        {
            return Result.Failure<CurrentAccount>(CurrentAccountErrors.PartyRequired);
        }

        return new CurrentAccount(Guid.NewGuid(), tenantId, partyId);
    }

    /// <summary>ORM/repository'nin var olan hareketleri geri yüklerken kullandığı iç ekleme yolu yoktur;
    /// hareketler EF navigation üzerinden materialize edilir. Bu metot yalnız test/senaryo kurulumları
    /// için değil, gerçek iş metotlarının ortak yardımcısıdır.</summary>
    private AccountEntry Append(
        EntryDirection direction,
        EntryType type,
        decimal amount,
        Guid? refId,
        DateTime occurredAt,
        DateTime? dueDate = null)
    {
        var entry = AccountEntry.Create(Id, TenantId, direction, type, Money.RoundToKurus(amount), refId, occurredAt, dueDate);
        _entries.Add(entry);
        return entry;
    }

    /// <summary>
    /// Alıcı carisine satış borcunu işler (docs/02 §5: alıcı cari BORÇ). Alıcının ödeyeceği brüt
    /// tutar deftere <see cref="EntryDirection.Debit"/>/<see cref="EntryType.Sale"/> olarak yazılır.
    /// Idempotency: aynı <paramref name="saleTransactionId"/> zaten işlenmişse hareket eklenmez
    /// (çift-kayıt koruması — docs/04 §5). Tutar pozitif olmalıdır.
    /// </summary>
    public Result RecordSaleDebit(Guid saleTransactionId, decimal grossAmount, DateTime occurredAt)
    {
        if (grossAmount <= 0m)
        {
            return Result.Failure(CurrentAccountErrors.NonPositiveAmount);
        }

        if (IsSaleAlreadyRecorded(saleTransactionId, EntryType.Sale))
        {
            // En-az-bir-kez teslimatta consumer yeniden tetiklenebilir; sessizce yut (idempotent).
            return Result.Success();
        }

        Append(EntryDirection.Debit, EntryType.Sale, grossAmount, saleTransactionId, occurredAt);
        return Result.Success();
    }

    /// <summary>
    /// Müstahsil carisine net hakedişi ALACAK olarak işler (docs/02 §5: müstahsil cari ALACAK +
    /// ödeme planı). Vade tarihi hareketin üzerinde saklanır (normal satış 15 iş günü — BK-3);
    /// vade yukarı katmandan gelir. <see cref="PaymentDue"/> event'i yayınlanır (Bildirim/AI için).
    /// Idempotency: aynı <paramref name="saleTransactionId"/> zaten işlenmişse eklenmez. Net tutar
    /// negatif olamaz (BK-1); sıfır hakediş kaydı da eklenmez.
    /// </summary>
    public Result RecordSettlementCredit(
        Guid saleTransactionId,
        decimal netAmount,
        DateTime dueDate,
        DateTime occurredAt)
    {
        if (netAmount < 0m)
        {
            return Result.Failure(CurrentAccountErrors.NegativeNet);
        }

        if (IsSaleAlreadyRecorded(saleTransactionId, EntryType.Settlement))
        {
            return Result.Success();
        }

        if (netAmount == 0m)
        {
            // Sıfır hakedişte defter hareketi/olay üretmeye gerek yok (değişmez korunur).
            return Result.Success();
        }

        Append(EntryDirection.Credit, EntryType.Settlement, netAmount, saleTransactionId, occurredAt, dueDate);

        RaiseDomainEvent(new PaymentDue(Id, TenantId, PartyId, Money.RoundToKurus(netAmount), dueDate, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Müstahsile ödeme yapıldığını işler (docs/02 §3.4 <c>PaymentMade</c>). Ödeme, müstahsilin
    /// ALACAĞINI azaltan bir BORÇ hareketidir (<see cref="EntryDirection.Debit"/>/
    /// <see cref="EntryType.Payment"/>). BK-6: nakit tutar 7.000 TL'yi aşamaz. Tutar pozitif olmalı.
    /// </summary>
    public Result RecordPayment(decimal amount, PaymentChannel channel, Guid? refId, DateTime occurredAt)
    {
        var guard = GuardCashLimit(amount, channel);
        if (guard.IsFailure)
        {
            return guard;
        }

        Append(EntryDirection.Debit, EntryType.Payment, amount, refId, occurredAt);

        RaiseDomainEvent(new PaymentMade(Id, TenantId, PartyId, Money.RoundToKurus(amount), DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Alıcıdan tahsilat alındığını işler (docs/02 §3.4 <c>CollectionReceived</c>). Tahsilat,
    /// alıcının BORCUNU azaltan bir ALACAK hareketidir (<see cref="EntryDirection.Credit"/>/
    /// <see cref="EntryType.Collection"/>). BK-6: nakit 7.000 TL'yi aşamaz. Tutar pozitif olmalı.
    /// </summary>
    public Result RecordCollection(decimal amount, PaymentChannel channel, Guid? refId, DateTime occurredAt)
    {
        var guard = GuardCashLimit(amount, channel);
        if (guard.IsFailure)
        {
            return guard;
        }

        Append(EntryDirection.Credit, EntryType.Collection, amount, refId, occurredAt);

        RaiseDomainEvent(new CollectionReceived(Id, TenantId, PartyId, Money.RoundToKurus(amount), DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Avans (peşin ödeme) işler (docs/02 §3.4: avans mahsuplaşır). Müstahsile verilen avans, ileride
    /// hakedişle mahsuplaşacak bir BORÇ hareketidir (<see cref="EntryDirection.Debit"/>/
    /// <see cref="EntryType.Advance"/>) — ödeme gibi alacağı azaltır. BK-6: nakit 7.000 TL'yi aşamaz.
    /// </summary>
    public Result RecordAdvance(decimal amount, PaymentChannel channel, Guid? refId, DateTime occurredAt)
    {
        var guard = GuardCashLimit(amount, channel);
        if (guard.IsFailure)
        {
            return guard;
        }

        Append(EntryDirection.Debit, EntryType.Advance, amount, refId, occurredAt);

        return Result.Success();
    }

    /// <summary>Bu satış (belirtilen türde) zaten deftere işlenmiş mi (idempotency).</summary>
    public bool IsSaleAlreadyRecorded(Guid saleTransactionId, EntryType type) =>
        _entries.Any(e => e.Type == type && e.RefId == saleTransactionId);

    /// <summary>Ortak tutar/kanal doğrulaması: pozitif tutar + BK-6 nakit eşiği (7.000 TL).</summary>
    private static Result GuardCashLimit(decimal amount, PaymentChannel channel)
    {
        if (amount <= 0m)
        {
            return Result.Failure(CurrentAccountErrors.NonPositiveAmount);
        }

        if (channel == PaymentChannel.Cash && amount > CashLimit)
        {
            return Result.Failure(CurrentAccountErrors.CashLimitExceeded);
        }

        return Result.Success();
    }

    /// <summary>BK-6 nakit eşiği: bu tutarı aşan nakit ödeme/tahsilat/avans yasaktır (banka zorunlu).</summary>
    public const decimal CashLimit = 7000m;
}

/// <summary>Cari hesap domain hataları (docs/07 §10; kod İngilizce, mesaj Türkçe — docs/07 §3).</summary>
public static class CurrentAccountErrors
{
    public static readonly Error PartyRequired =
        new("CurrentAccount.PartyRequired", "Cari hesap için taraf (party) referansı zorunludur.");

    public static readonly Error NonPositiveAmount =
        new("CurrentAccount.NonPositiveAmount", "Hareket tutarı sıfırdan büyük olmalıdır.");

    public static readonly Error NegativeNet =
        new("CurrentAccount.NegativeNet", "Müstahsil hakedişi (net) negatif olamaz.");

    public static readonly Error CashLimitExceeded =
        new("CurrentAccount.CashLimitExceeded",
            "7.000 TL üstü ödeme/tahsilat nakit yapılamaz; banka üzerinden ve belgeli olmalıdır (BK-6).");

    public static readonly Error NotFound =
        new("CurrentAccount.NotFound", "Cari hesap bulunamadı.");
}

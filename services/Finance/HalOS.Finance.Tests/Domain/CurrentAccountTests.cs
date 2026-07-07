using FluentAssertions;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Domain.Aggregates;
using HalOS.Finance.Domain.Enums;
using HalOS.Finance.Domain.Events;
using Xunit;

namespace HalOS.Finance.Tests.Domain;

/// <summary>
/// CurrentAccount çekirdek aggregate testleri (docs/02 §3.4, docs/03 §4 BK-1/BK-3/BK-6).
/// Bakiye = Σ hareket; alıcı satış BORÇ / müstahsil hakediş ALACAK yönleri; ödeme planı vade =
/// satış + 15 iş günü (BusinessDayCalculator ile doğru gün); 7.000 TL nakit eşiği (BK-6);
/// idempotency (aynı satış iki kez işlenmez). Saf, in-memory.
/// </summary>
public sealed class CurrentAccountTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _partyId = Guid.NewGuid();

    private static readonly DateTime SoldAt = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);

    private CurrentAccount NewAccount() => CurrentAccount.Open(_tenantId, _partyId).Value;

    [Fact]
    public void Open_MissingParty_Fails()
    {
        CurrentAccount.Open(_tenantId, Guid.Empty).Error
            .Should().Be(CurrentAccountErrors.PartyRequired);
    }

    [Fact]
    public void Open_NewAccount_StartsWithZeroBalanceAndNoEntries()
    {
        var account = NewAccount();

        account.Balance.Should().Be(0m);
        account.Entries.Should().BeEmpty();
        account.PartyId.Should().Be(_partyId);
        account.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void RecordSaleDebit_AddsPositiveBalance_BuyerOwesUs()
    {
        // docs/02 §5: alıcı carisine satış BORÇ (pozitif bakiye = tarafın işletmeye borcu).
        var account = NewAccount();

        var result = account.RecordSaleDebit(Guid.NewGuid(), 100.00m, SoldAt);

        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(100.00m);
        account.Entries.Should().ContainSingle();
        account.Entries.Single().Direction.Should().Be(EntryDirection.Debit);
        account.Entries.Single().Type.Should().Be(EntryType.Sale);
    }

    [Fact]
    public void RecordSettlementCredit_AddsNegativeBalance_WeOweProducer_AndRaisesPaymentDue()
    {
        // docs/02 §5: müstahsil carisine net hakediş ALACAK (negatif bakiye = işletmenin borcu).
        var account = NewAccount();
        var dueDate = new DateTime(2026, 7, 28);

        var result = account.RecordSettlementCredit(Guid.NewGuid(), 88.00m, dueDate, SoldAt);

        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(-88.00m);
        var entry = account.Entries.Single();
        entry.Direction.Should().Be(EntryDirection.Credit);
        entry.Type.Should().Be(EntryType.Settlement);
        entry.DueDate.Should().Be(dueDate);

        var evt = account.DomainEvents.OfType<PaymentDue>().Should().ContainSingle().Subject;
        evt.NetAmount.Should().Be(88.00m);
        evt.DueDate.Should().Be(dueDate);
        evt.ProducerPartyId.Should().Be(_partyId);
    }

    [Fact]
    public void Balance_IsSumOfEntries_DebitPlusCreditMinus()
    {
        // Değişmez: Balance = Σ SignedAmount (borç +, alacak −), kuruşa normalize (docs/02 §3.4).
        var account = NewAccount();

        account.RecordSaleDebit(Guid.NewGuid(), 100.00m, SoldAt);         // +100
        account.RecordCollection(40.00m, PaymentChannel.Bank, null, SoldAt); // −40 (alacak/tahsilat)

        // 100 − 40 = 60 (alıcının kalan borcu).
        account.Balance.Should().Be(60.00m);
        account.Entries.Should().HaveCount(2);
    }

    [Fact]
    public void SettlementDueDate_IsFifteenBusinessDays_MatchesBusinessDayCalculator()
    {
        // BK-3: normal satış ödeme planı = satış + 15 İŞ GÜNÜ. Vade yukarı katmandan gelir; burada
        // hesabın doğru günü sakladığını BusinessDayCalculator ile birebir doğrularız.
        var account = NewAccount();
        var expectedDue = BusinessDayCalculator.AddBusinessDays(SoldAt, 15);

        account.RecordSettlementCredit(Guid.NewGuid(), 88.00m, expectedDue, SoldAt);

        var entry = account.Entries.Single();
        entry.DueDate.Should().Be(expectedDue);
        // 2026-07-06 Pzt başlangıç; aralıkta 15 Temmuz resmi tatili atlanır → 2026-07-28 Salı.
        entry.DueDate!.Value.Date.Should().Be(new DateTime(2026, 7, 28));
        entry.DueDate.Value.DayOfWeek.Should().NotBe(DayOfWeek.Saturday);
        entry.DueDate.Value.DayOfWeek.Should().NotBe(DayOfWeek.Sunday);
    }

    [Fact]
    public void RecordSaleDebit_SameSaleTwice_IsIdempotent_NoDuplicate()
    {
        // docs/04 §5/§10: en-az-bir-kez teslimatta consumer yeniden tetiklenebilir; aynı satış
        // ikinci kez hareket eklememelidir (çift-kayıt koruması).
        var account = NewAccount();
        var saleId = Guid.NewGuid();

        account.RecordSaleDebit(saleId, 100.00m, SoldAt);
        var second = account.RecordSaleDebit(saleId, 100.00m, SoldAt);

        second.IsSuccess.Should().BeTrue();
        account.Entries.Should().ContainSingle();
        account.Balance.Should().Be(100.00m);
    }

    [Fact]
    public void RecordSettlementCredit_SameSaleTwice_IsIdempotent_NoDuplicate()
    {
        var account = NewAccount();
        var saleId = Guid.NewGuid();
        var due = new DateTime(2026, 7, 28);

        account.RecordSettlementCredit(saleId, 88.00m, due, SoldAt);
        account.RecordSettlementCredit(saleId, 88.00m, due, SoldAt);

        account.Entries.Should().ContainSingle();
        account.Balance.Should().Be(-88.00m);
        account.DomainEvents.OfType<PaymentDue>().Should().ContainSingle();
    }

    [Fact]
    public void RecordSettlementCredit_NegativeNet_Fails_InvariantHeld()
    {
        // BK-1 değişmez: hakediş (net) negatif olamaz.
        var account = NewAccount();

        var result = account.RecordSettlementCredit(Guid.NewGuid(), -1m, new DateTime(2026, 7, 28), SoldAt);

        result.Error.Should().Be(CurrentAccountErrors.NegativeNet);
        account.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(7000, true)]    // eşik dahil kabul.
    [InlineData(7000.01, false)] // eşik üstü nakit reddedilir (BK-6).
    [InlineData(9999.99, false)]
    public void RecordPayment_Cash_RespectsSevenThousandLimit_BK6(decimal amount, bool shouldSucceed)
    {
        var account = NewAccount();

        var result = account.RecordPayment(amount, PaymentChannel.Cash, null, SoldAt);

        result.IsSuccess.Should().Be(shouldSucceed);
        if (!shouldSucceed)
        {
            result.Error.Should().Be(CurrentAccountErrors.CashLimitExceeded);
            account.Entries.Should().BeEmpty();
        }
    }

    [Fact]
    public void RecordPayment_BankAboveLimit_Succeeds_BK6()
    {
        // BK-6: eşik yalnız nakit için; banka üzerinden büyük tutar serbest (belgeli).
        var account = NewAccount();

        var result = account.RecordPayment(50000.00m, PaymentChannel.Bank, null, SoldAt);

        result.IsSuccess.Should().BeTrue();
        account.Entries.Single().Direction.Should().Be(EntryDirection.Debit);
        account.Entries.Single().Type.Should().Be(EntryType.Payment);
        account.DomainEvents.OfType<PaymentMade>().Should().ContainSingle();
    }

    [Fact]
    public void RecordCollection_RaisesCollectionReceived_AndReducesBuyerBalance()
    {
        var account = NewAccount();
        account.RecordSaleDebit(Guid.NewGuid(), 100.00m, SoldAt); // borç +100

        var result = account.RecordCollection(100.00m, PaymentChannel.Bank, null, SoldAt);

        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(0m); // borç kapandı.
        account.DomainEvents.OfType<CollectionReceived>().Should().ContainSingle();
    }

    [Fact]
    public void RecordAdvance_IsDebit_ReducesProducerCredit_Mahsup()
    {
        // docs/02 §3.4: avans mahsuplaşır. Müstahsil hakedişi (alacak −) üzerine verilen avans
        // (borç +) net borcu azaltır.
        var account = NewAccount();
        account.RecordSettlementCredit(Guid.NewGuid(), 88.00m, new DateTime(2026, 7, 28), SoldAt); // −88

        var result = account.RecordAdvance(30.00m, PaymentChannel.Bank, null, SoldAt); // +30

        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(-58.00m); // işletmenin müstahsile kalan borcu.
        account.Entries.Single(e => e.Type == EntryType.Advance).Direction.Should().Be(EntryDirection.Debit);
    }

    [Fact]
    public void RecordPayment_NonPositiveAmount_Fails()
    {
        var account = NewAccount();

        account.RecordPayment(0m, PaymentChannel.Bank, null, SoldAt).Error
            .Should().Be(CurrentAccountErrors.NonPositiveAmount);
        account.RecordPayment(-5m, PaymentChannel.Bank, null, SoldAt).Error
            .Should().Be(CurrentAccountErrors.NonPositiveAmount);
    }
}

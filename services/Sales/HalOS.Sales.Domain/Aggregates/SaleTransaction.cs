using HalOS.BuildingBlocks.Contracts;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Domain.Enums;
using HalOS.Sales.Domain.Events;
using HalOS.Sales.Domain.Services;
using HalOS.Sales.Domain.ValueObjects;

namespace HalOS.Sales.Domain.Aggregates;

/// <summary>
/// Satış Kaydı (SaleTransaction) — ÇEKİRDEK aggregate (docs/02 §3.3; docs/05 §3.5). Bir alıcıya
/// yapılan tek satış işlemini, satırlarını ve tamamlanınca kesinti/hakediş sonucunu tutar.
/// Tenant'a bağlıdır (ITenantOwned → global query filter, BK-8). Taraf/kaynak referansları ID
/// ile (servisler arası FK yok — docs/05 §5).
///
/// Değişmezler (docs/02 §3.3, docs/03 §4 BK-1/BK-9):
/// - <c>gross = Σ SaleLine.LineAmount</c>.
/// - Kesintiler brüt üzerinden hesaplanır; net = brüt − (komisyon + stopaj + Bağ-Kur + rüsum).
/// - <see cref="Complete"/> çağrılınca CommissionCalculation + Deduction'lar + Settlement üretilir
///   ve <see cref="SaleCompleted"/> event'i yayınlanır (outbox'a atomik yazılır — docs/04 §10).
/// - Tamamlanmış satış SİLİNMEZ; iptal ters kayıt/flag ile (BK-9): <see cref="Cancel"/>.
/// - <see cref="OperationId"/> offline idempotency içindir (docs/04 §5).
/// </summary>
public sealed class SaleTransaction : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<SaleLine> _lines = new();
    private readonly List<Deduction> _deductions = new();

    private SaleTransaction(
        Guid id,
        Guid tenantId,
        Guid buyerPartyId,
        Guid producerPartyId,
        Guid? consignmentId,
        DateTime soldAt,
        bool isWithinMarket,
        Guid operationId,
        Guid createdBy,
        DateTime createdOnUtc)
        : base(id)
    {
        TenantId = tenantId;
        BuyerPartyId = buyerPartyId;
        ProducerPartyId = producerPartyId;
        ConsignmentId = consignmentId;
        SoldAt = soldAt;
        IsWithinMarket = isWithinMarket;
        OperationId = operationId;
        CreatedBy = createdBy;
        CreatedOnUtc = createdOnUtc;
        Status = SaleStatus.Draft;
        GrossAmount = 0m;
        IsCancelled = false;
    }

    /// <summary>ORM materialization only.</summary>
    private SaleTransaction()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>Alıcı referansı (Party ID — FK değil, docs/05 §5).</summary>
    public Guid BuyerPartyId { get; private set; }

    /// <summary>Müstahsil referansı (Party ID — hakediş bu tarafa; FK değil, docs/05 §5).</summary>
    public Guid ProducerPartyId { get; private set; }

    /// <summary>Kaynak mal geliş referansı (tüccar kendi malını satarsa null — docs/05 §3.5).</summary>
    public Guid? ConsignmentId { get; private set; }

    public DateTime SoldAt { get; private set; }

    /// <summary>Brüt satış bedeli = Σ satır tutarı (NUMERIC(18,2), BK-1).</summary>
    public decimal GrossAmount { get; private set; }

    /// <summary>Hal içi/dışı satış → rüsum oranı %1/%2 (docs/05 §3.5, BK-5).</summary>
    public bool IsWithinMarket { get; private set; }

    public SaleStatus Status { get; private set; }

    /// <summary>Offline idempotency anahtarı (client-generated; docs/04 §5).</summary>
    public Guid OperationId { get; private set; }

    /// <summary>İptal bayrağı — ters kayıt/denetim izi için (docs/05 §1, BK-9).</summary>
    public bool IsCancelled { get; private set; }

    /// <summary>İptal gerekçesi (BK-9 denetim izi).</summary>
    public string? CancellationReason { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public IReadOnlyCollection<SaleLine> Lines => _lines.AsReadOnly();

    /// <summary>Komisyon hesabı (satışla 1:1; tamamlanınca üretilir).</summary>
    public CommissionCalculation? CommissionCalculation { get; private set; }

    public IReadOnlyCollection<Deduction> Deductions => _deductions.AsReadOnly();

    /// <summary>Müstahsile hakediş (satışla 1:1; tamamlanınca üretilir).</summary>
    public Settlement? Settlement { get; private set; }

    /// <summary>
    /// Yeni bir taslak (Draft) satış kaydı açar (docs/03 M4). Alıcı ve müstahsil referansları
    /// zorunlu. Satırlar <see cref="AddLine"/> ile eklenir; kesinti/hakediş <see cref="Complete"/>
    /// ile hesaplanır.
    /// </summary>
    public static Result<SaleTransaction> Create(
        Guid tenantId,
        Guid buyerPartyId,
        Guid producerPartyId,
        Guid? consignmentId,
        DateTime soldAt,
        bool isWithinMarket,
        Guid operationId,
        Guid createdBy)
    {
        if (buyerPartyId == Guid.Empty)
        {
            return Result.Failure<SaleTransaction>(SaleErrors.BuyerRequired);
        }

        if (producerPartyId == Guid.Empty)
        {
            return Result.Failure<SaleTransaction>(SaleErrors.ProducerRequired);
        }

        var effectiveOperationId = operationId == Guid.Empty ? Guid.NewGuid() : operationId;

        return new SaleTransaction(
            Guid.NewGuid(),
            tenantId,
            buyerPartyId,
            producerPartyId,
            consignmentId,
            soldAt,
            isWithinMarket,
            effectiveOperationId,
            createdBy,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Taslak satışa bir satır ekler (docs/03 M4). Yalnızca Draft durumunda; miktar &gt; 0,
    /// birim fiyat ≥ 0 olmalı. Brüt bedel (<see cref="GrossAmount"/>) satır tutarlarının toplamı
    /// olarak yeniden hesaplanır (BK-1).
    /// </summary>
    public Result AddLine(Guid productId, decimal quantity, UnitOfMeasure unit, decimal unitPrice)
    {
        if (Status != SaleStatus.Draft)
        {
            return Result.Failure(SaleErrors.NotDraft);
        }

        if (productId == Guid.Empty)
        {
            return Result.Failure(SaleErrors.ProductRequired);
        }

        if (quantity <= 0m)
        {
            return Result.Failure(SaleErrors.InvalidQuantity);
        }

        if (unitPrice < 0m)
        {
            return Result.Failure(SaleErrors.InvalidUnitPrice);
        }

        _lines.Add(SaleLine.Create(Id, TenantId, productId, quantity, unit, unitPrice));
        RecalculateGross();

        return Result.Success();
    }

    /// <summary>
    /// Satışı tamamlar ve kesinti/hakediş motorunu çalıştırır (docs/02 §4, docs/03 §4 BK-1/BK-2/BK-3).
    /// Yalnızca en az bir satırı olan Draft satış tamamlanabilir. Motor sonucundan
    /// CommissionCalculation + Deduction'lar (commission/agri_withholding/farmer_ssk/market_fee/vat)
    /// + Settlement üretilir; <see cref="SaleCompleted"/> event'i yayınlanır.
    ///
    /// Hakediş vade tarihi = <see cref="SoldAt"/> + 15 iş günü (hafta sonu atlanır; resmi tatil
    /// kapsam dışı — <see cref="BusinessDayCalculator"/> notu, BK-3). Net negatif çıkarsa
    /// <c>Settlement.Create</c> reddeder ve Complete hatayla döner (değişmez korunur).
    /// </summary>
    public Result Complete(RateSet rates)
    {
        if (Status == SaleStatus.Completed)
        {
            return Result.Failure(SaleErrors.AlreadyCompleted);
        }

        if (Status == SaleStatus.Cancelled)
        {
            return Result.Failure(SaleErrors.CancelledSaleCannotComplete);
        }

        if (_lines.Count == 0)
        {
            return Result.Failure(SaleErrors.NoLines);
        }

        RecalculateGross();

        var calculation = SettlementCalculator.Calculate(GrossAmount, rates);

        // Hakediş vadesi: satış + 15 iş günü (BK-3). Hafta sonu atlanır; resmi tatil MVP dışı.
        var dueDate = BusinessDayCalculator.AddBusinessDays(SoldAt, SettlementDueBusinessDays);

        var settlementResult = Settlement.Create(Id, TenantId, calculation.Net, dueDate);
        if (settlementResult.IsFailure)
        {
            // Net negatif → hakediş değişmezi ihlali; satış tamamlanamaz (BK-1).
            return Result.Failure(settlementResult.Error);
        }

        CommissionCalculation = CommissionCalculation.Create(
            Id,
            TenantId,
            rates.CommissionRate,
            calculation.Commission,
            rates.VatRate,
            calculation.VatOnCommission);

        BuildDeductions(calculation);

        Settlement = settlementResult.Value;
        Status = SaleStatus.Completed;

        RaiseDomainEvent(new SaleCompleted(
            Id,
            TenantId,
            BuyerPartyId,
            ProducerPartyId,
            SoldAt,
            calculation.Gross,
            calculation.Commission,
            calculation.TotalDeductions,
            calculation.Net,
            dueDate,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Satışı iptal eder (docs/03 §4 BK-9). Tamamlanmış ve belgesi kesilmiş satış SİLİNMEZ; durum
    /// Cancelled'a çekilir, <see cref="IsCancelled"/> işaretlenir, gerekçe saklanır (denetim izi
    /// korunur — ters kayıt Finance/e-Belge tarafında SaleCancelled event'iyle atılır). İptal
    /// idempotent değildir: zaten iptal edilmiş satış tekrar iptal edilemez.
    /// </summary>
    public Result Cancel(string reason)
    {
        if (Status == SaleStatus.Cancelled)
        {
            return Result.Failure(SaleErrors.AlreadyCancelled);
        }

        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Belirtilmedi" : reason.Trim();

        Status = SaleStatus.Cancelled;
        IsCancelled = true;
        CancellationReason = normalizedReason;

        RaiseDomainEvent(new SaleCancelled(Id, TenantId, normalizedReason, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>Müstahsile ödeme süresi (docs/03 §4 BK-3): normal satışta 15 iş günü.</summary>
    public const int SettlementDueBusinessDays = 15;

    private void RecalculateGross() =>
        GrossAmount = Money.RoundToKurus(_lines.Sum(l => l.LineAmount));

    private void BuildDeductions(SettlementCalculation calculation)
    {
        _deductions.Clear();

        // Komisyon ve rüsum AYRI kalemler; KDV ayrı satır (docs/02 §7 anti-pattern, docs/05 §3.5).
        _deductions.Add(Deduction.Create(Id, TenantId, DeductionType.Commission, calculation.Rates.CommissionRate, calculation.Commission));
        _deductions.Add(Deduction.Create(Id, TenantId, DeductionType.AgriWithholding, calculation.Rates.AgriWithholdingRate, calculation.AgriWithholding));
        _deductions.Add(Deduction.Create(Id, TenantId, DeductionType.FarmerSsk, calculation.Rates.FarmerSskRate, calculation.FarmerSsk));
        _deductions.Add(Deduction.Create(Id, TenantId, DeductionType.MarketFee, calculation.Rates.MarketFeeRate, calculation.MarketFee));
        // KDV kesinti kaydı: hakedişten düşülmez ama ayrı kalem olarak izlenir (BK-1, docs/05 §3.5).
        _deductions.Add(Deduction.Create(Id, TenantId, DeductionType.Vat, calculation.Rates.VatRate, calculation.VatOnCommission));
    }
}

public static class SaleErrors
{
    public static readonly Error BuyerRequired =
        new("Sale.BuyerRequired", "Alıcı referansı zorunludur.");

    public static readonly Error ProducerRequired =
        new("Sale.ProducerRequired", "Müstahsil referansı zorunludur.");

    public static readonly Error ProductRequired =
        new("Sale.ProductRequired", "Satır için ürün referansı zorunludur.");

    public static readonly Error InvalidQuantity =
        new("Sale.InvalidQuantity", "Satır miktarı sıfırdan büyük olmalıdır.");

    public static readonly Error InvalidUnitPrice =
        new("Sale.InvalidUnitPrice", "Birim fiyat negatif olamaz.");

    public static readonly Error NotDraft =
        new("Sale.NotDraft", "Yalnızca taslak (Draft) satışa satır eklenebilir.");

    public static readonly Error NoLines =
        new("Sale.NoLines", "Tamamlanacak satışta en az bir satır olmalıdır.");

    public static readonly Error AlreadyCompleted =
        new("Sale.AlreadyCompleted", "Satış zaten tamamlanmış.");

    public static readonly Error AlreadyCancelled =
        new("Sale.AlreadyCancelled", "Satış zaten iptal edilmiş.");

    public static readonly Error CancelledSaleCannotComplete =
        new("Sale.CancelledSaleCannotComplete", "İptal edilmiş satış tamamlanamaz.");

    public static readonly Error NotFound =
        new("Sale.NotFound", "Satış kaydı bulunamadı.");

    public static readonly Error DuplicateOperation =
        new("Sale.DuplicateOperation", "Bu işlem (operationId) zaten kaydedilmiş.");
}

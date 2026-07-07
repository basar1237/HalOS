using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Domain.Enums;
using HalOS.Integration.Domain.Events;
using HalOS.Integration.Domain.ValueObjects;

namespace HalOS.Integration.Domain.Aggregates;

/// <summary>
/// HKS Bildirimi (<c>HksNotification</c>) — e-Belge &amp; Yasal Entegrasyon bağlamının kök aggregate'i
/// (docs/02 §1.2 / §3.5 <c>LegalDocument</c> alt tipi; docs/03 M8 / BK-4; docs/04 ADR-010). Alım-satım/
/// aracılık faaliyetinin HKS'e (Hal Kayıt Sistemi) raporudur (docs/02 §1.2). Tenant'a bağlıdır
/// (ITenantOwned → global query filter, BK-8). Taraf/satış referansları ID ile (servisler arası FK yok
/// — docs/05 §5).
///
/// Değişmezler (docs/02 §3.5, docs/03 BK-4/BK-5):
/// - HER başarılı satış HKS'e bildirilir (BK-4 değişmez); üretim KeepsRecords'a bağlı DEĞİLDİR (e-MM'in
///   aksine). Brüt, komisyon ve hal rüsumu bildirim gövdesinde AYRI taşınır (rüsum tek "fee" altında
///   birleştirilmez — docs/02 §7); tutarlar YENİDEN HESAPLANMAZ, SaleCompleted'tan gelir (docs/04 §10).
/// - Bir satış (<see cref="SaleTransactionId"/>) tenant içinde en fazla BİR HKS bildirimi üretir
///   (idempotency anahtarı — docs/04 §5/§10). Consumer bu tekilliğe dayanır.
/// - Tamamlanmış/yasal belge SİLİNMEZ; iptal durum bayrağıyla (Cancelled — BK-9).
///
/// Tüm tutarlar <see cref="decimal"/> ve kuruşa yuvarlıdır (BK-2). Bildirim gönderilince
/// <see cref="HksNotified"/> event'i yayınlanır (outbox'a atomik — docs/04 §10). e-MM
/// (<see cref="ProducerReceipt"/>) aggregate deseniyle birebir.
/// </summary>
public sealed class HksNotification : AggregateRoot<Guid>, ITenantOwned
{
    private HksNotification(
        Guid id,
        Guid tenantId,
        Guid saleTransactionId,
        Guid buyerPartyId,
        Guid producerPartyId,
        DateTime notifiedDate,
        decimal grossAmount,
        decimal commissionAmount,
        decimal marketFeeAmount)
        : base(id)
    {
        TenantId = tenantId;
        SaleTransactionId = saleTransactionId;
        BuyerPartyId = buyerPartyId;
        ProducerPartyId = producerPartyId;
        NotifiedDate = notifiedDate;
        GrossAmount = grossAmount;
        CommissionAmount = commissionAmount;
        MarketFeeAmount = marketFeeAmount;
        Status = HksNotificationStatus.Draft;
    }

    /// <summary>ORM materialization only.</summary>
    private HksNotification()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>Bildirimin kaynağı satış (idempotency anahtarı — tenant içinde tekil; FK değil, docs/05 §5).</summary>
    public Guid SaleTransactionId { get; private set; }

    /// <summary>Alıcı referansı (Party ID; FK değil, docs/05 §5). Bildirimde taraf bilgisi.</summary>
    public Guid BuyerPartyId { get; private set; }

    /// <summary>Müstahsil referansı (Party ID; FK değil, docs/05 §5). Bildirimde taraf bilgisi.</summary>
    public Guid ProducerPartyId { get; private set; }

    /// <summary>Bildirim tarihi (satışın gerçekleştiği an).</summary>
    public DateTime NotifiedDate { get; private set; }

    /// <summary>Brüt satış bedeli — HKS'e bildirilir (NUMERIC(18,2), BK-2).</summary>
    public decimal GrossAmount { get; private set; }

    /// <summary>Komisyon tutarı — HKS'e bildirilir (NUMERIC(18,2), BK-1).</summary>
    public decimal CommissionAmount { get; private set; }

    /// <summary>Hal rüsumu tutarı — HKS'e bildirilir; belediyeye AYRI raporlanır (NUMERIC(18,2), BK-5).</summary>
    public decimal MarketFeeAmount { get; private set; }

    /// <summary>HKS referans numarası — bildirim gönderilince (Notified) atanır; öncesinde null.</summary>
    public string? ReferenceNumber { get; private set; }

    public HksNotificationStatus Status { get; private set; }

    /// <summary>
    /// Bir satıştan (SaleCompleted) yeni bir HKS bildirimi taslağı (Draft) üretir (docs/03 M8 / BK-4).
    /// Tutarlar YENİDEN HESAPLANMAZ; Sales'in taşıdığı brüt + komisyon + hal rüsumu (event'ten) doğrudan
    /// kullanılır (docs/04 §10 event-taşımalı). Brüt pozitif olmalı; komisyon/rüsum negatif olamaz.
    /// </summary>
    /// <remarks>
    /// HKS bildirimi HER satış için üretilir (BK-4 değişmez: her satış → HKS bildirimi); e-MM'in aksine
    /// KeepsRecords koşuluna bağlı DEĞİLDİR. Bu factory yalnız tutar/işaret değişmezlerini korur.
    /// </remarks>
    public static Result<HksNotification> Create(
        Guid tenantId,
        Guid saleTransactionId,
        Guid buyerPartyId,
        Guid producerPartyId,
        DateTime notifiedDate,
        decimal grossAmount,
        decimal commissionAmount,
        decimal marketFeeAmount)
    {
        if (saleTransactionId == Guid.Empty)
        {
            return Result.Failure<HksNotification>(HksNotificationErrors.SaleRequired);
        }

        if (grossAmount <= 0m)
        {
            return Result.Failure<HksNotification>(HksNotificationErrors.NonPositiveGross);
        }

        if (commissionAmount < 0m || marketFeeAmount < 0m)
        {
            return Result.Failure<HksNotification>(HksNotificationErrors.NegativeAmount);
        }

        return new HksNotification(
            Guid.NewGuid(),
            tenantId,
            saleTransactionId,
            buyerPartyId,
            producerPartyId,
            notifiedDate,
            Money.RoundToKurus(grossAmount),
            Money.RoundToKurus(commissionAmount),
            Money.RoundToKurus(marketFeeAmount));
    }

    /// <summary>
    /// Bildirimi HKS'e gönderilmiş olarak işaretler ve referans numarasını atar (docs/02 §3.5
    /// <c>HksNotified</c>; docs/04 ADR-007 gönderim). <see cref="HksNotified"/> event'i yayınlanır
    /// (outbox'a atomik — docs/04 §10). Yalnız Draft/Failed durumundan Notified'a geçilebilir; tekrar
    /// çağrı (idempotent) zararsızdır (zaten Notified ise event tekrar üretilmez). Referans numarası
    /// zorunludur. e-MM.<c>MarkIssued</c> deseniyle birebir.
    /// </summary>
    public Result MarkNotified(string referenceNumber)
    {
        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            return Result.Failure(HksNotificationErrors.ReferenceNumberRequired);
        }

        if (Status == HksNotificationStatus.Cancelled)
        {
            return Result.Failure(HksNotificationErrors.CancelledCannotNotify);
        }

        if (Status == HksNotificationStatus.Notified)
        {
            // Zaten gönderilmiş — idempotent tekrar; yeni event üretme (docs/04 §5).
            return Result.Success();
        }

        ReferenceNumber = referenceNumber.Trim();
        Status = HksNotificationStatus.Notified;

        RaiseDomainEvent(new HksNotified(
            Id,
            TenantId,
            SaleTransactionId,
            ReferenceNumber,
            GrossAmount,
            MarketFeeAmount,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Bildirimi gönderim başarısız olarak işaretler (docs/02 §3.5 <c>DocumentRejected</c>; docs/03 BK-4).
    /// Kullanıcı uyarılır; ADR-007 retry ile yeniden denenebilir. İptal edilmiş bildirim için geçersizdir.
    /// e-MM.<c>MarkFailed</c> deseniyle birebir.
    /// </summary>
    public Result MarkFailed()
    {
        if (Status == HksNotificationStatus.Cancelled)
        {
            return Result.Failure(HksNotificationErrors.CancelledCannotNotify);
        }

        Status = HksNotificationStatus.Failed;
        return Result.Success();
    }

    /// <summary>HKS bildirimi domain hataları (docs/07 §10; kod İngilizce, mesaj Türkçe — docs/07 §3).</summary>
    public static class HksNotificationErrors
    {
        public static readonly Error SaleRequired =
            new("HksNotification.SaleRequired", "HKS bildirimi için satış referansı zorunludur.");

        public static readonly Error NonPositiveGross =
            new("HksNotification.NonPositiveGross", "Brüt tutar sıfırdan büyük olmalıdır.");

        public static readonly Error NegativeAmount =
            new("HksNotification.NegativeAmount", "Komisyon/rüsum tutarları negatif olamaz.");

        public static readonly Error ReferenceNumberRequired =
            new("HksNotification.ReferenceNumberRequired", "HKS referans numarası zorunludur.");

        public static readonly Error CancelledCannotNotify =
            new("HksNotification.CancelledCannotNotify", "İptal edilmiş bildirim gönderilemez.");

        public static readonly Error NotFound =
            new("HksNotification.NotFound", "HKS bildirimi bulunamadı.");
    }
}

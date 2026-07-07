using HalOS.BuildingBlocks.Contracts;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Integration.Application.Consumers;

/// <summary>
/// Sales servisinden gelen <see cref="SaleCompleted"/>'i tüketip zorunlu yasal belgeleri üretir
/// (docs/02 §3.5 / §6: SaleCompleted → e-Belge; docs/03 M7/M8 / BK-4; docs/04 ADR-007/ADR-010/§10).
///
/// <b>Değişmez (docs/02 §3.5, docs/03 BK-4):</b> her başarılı satış → <b>en az bir e-Fatura + HKS
/// bildirimi</b> + (müstahsil kayıt tutmuyorsa) e-MM. Bu consumer üç belgeyi de tek tüketimde,
/// idempotent olarak üretir:
/// <list type="bullet">
///   <item><b>e-Fatura (HAL, KOMİSYON)</b> — HER satış için üretilir; alıcıya (BuyerPartyId) kesilir;
///     tutar = komisyon + komisyon KDV'si (SaleCompleted taşır, yeniden hesap YOK — docs/02 §1.2/§4).</item>
///   <item><b>HKS bildirimi</b> — HER satış için üretilir (BK-4 "her satış HKS'e bildirilir");
///     brüt + komisyon + hal rüsumu taşınır (rüsum AYRI — docs/02 §7, BK-5).</item>
///   <item><b>e-MM</b> — YALNIZ kayıt TUTMAYAN müstahsil için (aşağıdaki KeepsRecords kararı, BK-4).</item>
/// </list>
///
/// <b>e-MM üretim kararı (BK-4):</b> Müstahsilin <c>KeepsRecords</c> bilgisi Party event'inden gelen
/// <see cref="Domain.ReadModels.ProducerTaxProfile"/> okuma modelinden okunur:
/// <list type="bullet">
///   <item>Profil yok (henüz senkronlanmamış) → e-MM gerekliliğine KARAR VERİLEMEZ; e-Fatura+HKS zaten
///     üretilip kalıcılaştıktan sonra İSTİSNA fırlatılır → MassTransit retry/error queue devreye girer
///     (docs/04 §10 en-az-bir-kez). Retry'da e-Fatura+HKS idempotent atlanır, e-MM yeniden denenir.</item>
///   <item>KeepsRecords = true → e-MM ÜRETME (kayıt tutan müstahsile e-MM düzenlenmez; info log).</item>
///   <item>KeepsRecords = false → e-MM ÜRET (idempotent, SaleTransactionId ile).</item>
/// </list>
///
/// <b>Yeniden hesap YASAK (docs/07 §5):</b> tüm tutarlar SaleCompleted'tan gelir (Sales tek gerçeklik
/// kaynağı — docs/04 §10 event-taşımalı). e-MM'e YALNIZ stopaj + Bağ-Kur girer (komisyon/rüsum/KDV
/// girmez — BK-1); e-Fatura'ya komisyon + KDV; HKS'e brüt + komisyon + rüsum.
///
/// <b>Idempotency</b> (docs/04 §5): her belge türü satış başına en fazla BİR kez üretilir (aynı
/// SaleCompleted tekrar gelse ikinci belge oluşmaz). <b>Tenant</b>: mesajdan
/// (<see cref="ITenantScopedEvent"/>) <c>TenantConsumeFilter</c> ile ambient tenant'a set edilir; DbContext
/// global query filter DOĞRU tenant'ta çalışır (docs/07 §6 / BK-8). El-yapımı outbox korunur.
///
/// <b>Yutulan Result yok</b>: gateway veya domain IsFailure ise SaveChanges'ten ÖNCE anlamlı istisna
/// fırlatılır → MassTransit retry/error queue devreye girer (docs/04 §10 en-az-bir-kez); belge yarım
/// kalmaz / sessizce ack'lenmez (BK-1/BK-4).
/// </summary>
public sealed class SaleCompletedConsumer : IConsumer<SaleCompleted>
{
    private readonly IProducerReceiptRepository _receipts;
    private readonly IInvoiceRepository _invoices;
    private readonly IHksNotificationRepository _notifications;
    private readonly IProducerTaxProfileReader _profiles;
    private readonly IEDocumentGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaleCompletedConsumer> _logger;

    public SaleCompletedConsumer(
        IProducerReceiptRepository receipts,
        IInvoiceRepository invoices,
        IHksNotificationRepository notifications,
        IProducerTaxProfileReader profiles,
        IEDocumentGateway gateway,
        IUnitOfWork unitOfWork,
        ILogger<SaleCompletedConsumer> logger)
    {
        _receipts = receipts;
        _invoices = invoices;
        _notifications = notifications;
        _profiles = profiles;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SaleCompleted> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        // Her başarılı satış → e-Fatura + HKS bildirimi (BK-4 değişmez; KeepsRecords'tan bağımsız).
        await ProduceInvoiceAsync(message, ct);
        await ProduceHksNotificationAsync(message, ct);

        // e-MM YALNIZ kayıt tutmayan müstahsil için (BK-4).
        await ProduceProducerReceiptAsync(message, ct);
    }

    /// <summary>
    /// Her satış için KOMİSYON e-Faturası üretir (BK-4). Alıcıya kesilir; tutar SaleCompleted'ın
    /// komisyon + komisyon KDV'sinden gelir (yeniden hesap yok). Idempotent (SaleTransactionId).
    /// </summary>
    private async Task ProduceInvoiceAsync(SaleCompleted message, CancellationToken ct)
    {
        var existing = await _invoices.GetBySaleTransactionIdAsync(message.SaleTransactionId, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "e-Fatura zaten mevcut, atlandı (idempotent): Tenant={TenantId} Sale={SaleTransactionId}.",
                message.TenantId,
                message.SaleTransactionId);
            return;
        }

        var createResult = Invoice.CreateCommission(
            message.TenantId,
            message.SaleTransactionId,
            message.BuyerPartyId,
            message.SoldAt,
            message.CommissionAmount,
            message.CommissionVatAmount);

        if (createResult.IsFailure)
        {
            _logger.LogError(
                "e-Fatura üretilemedi (reddedildi): Tenant={TenantId} Sale={SaleTransactionId} Hata={ErrorCode} — {ErrorMessage}.",
                message.TenantId,
                message.SaleTransactionId,
                createResult.Error.Code,
                createResult.Error.Message);

            throw new InvalidOperationException(
                $"e-Fatura {message.SaleTransactionId} üretilemedi: {createResult.Error}");
        }

        var invoice = createResult.Value;

        var sendResult = await _gateway.SendInvoiceAsync(invoice, ct);
        if (sendResult.IsFailure)
        {
            _logger.LogError(
                "e-Fatura gönderimi başarısız: Tenant={TenantId} Sale={SaleTransactionId} Hata={ErrorCode} — {ErrorMessage}.",
                message.TenantId,
                message.SaleTransactionId,
                sendResult.Error.Code,
                sendResult.Error.Message);

            throw new InvalidOperationException(
                $"e-Fatura {message.SaleTransactionId} gönderimi başarısız: {sendResult.Error}");
        }

        var issueResult = invoice.MarkIssued(sendResult.Value);
        if (issueResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"e-Fatura {message.SaleTransactionId} kesilemedi: {issueResult.Error}");
        }

        _invoices.Add(invoice);

        // InvoiceIssued event'i SaveChanges içinde tenant'lı olarak outbox'a atomik yazılır (docs/04 §10).
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "e-Fatura kesildi: Tenant={TenantId} Sale={SaleTransactionId} Buyer={BuyerPartyId} " +
            "FaturaNo={InvoiceNumber} Toplam={TotalAmount}.",
            message.TenantId,
            message.SaleTransactionId,
            message.BuyerPartyId,
            invoice.InvoiceNumber,
            invoice.TotalAmount);
    }

    /// <summary>
    /// Her satış için HKS bildirimi üretir (BK-4 "her satış HKS'e bildirilir"). Brüt + komisyon + hal
    /// rüsumu taşınır (SaleCompleted'tan; yeniden hesap yok). Idempotent (SaleTransactionId).
    /// </summary>
    private async Task ProduceHksNotificationAsync(SaleCompleted message, CancellationToken ct)
    {
        var existing = await _notifications.GetBySaleTransactionIdAsync(message.SaleTransactionId, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "HKS bildirimi zaten mevcut, atlandı (idempotent): Tenant={TenantId} Sale={SaleTransactionId}.",
                message.TenantId,
                message.SaleTransactionId);
            return;
        }

        var createResult = HksNotification.Create(
            message.TenantId,
            message.SaleTransactionId,
            message.BuyerPartyId,
            message.ProducerPartyId,
            message.SoldAt,
            message.GrossAmount,
            message.CommissionAmount,
            message.MarketFeeAmount);

        if (createResult.IsFailure)
        {
            _logger.LogError(
                "HKS bildirimi üretilemedi (reddedildi): Tenant={TenantId} Sale={SaleTransactionId} Hata={ErrorCode} — {ErrorMessage}.",
                message.TenantId,
                message.SaleTransactionId,
                createResult.Error.Code,
                createResult.Error.Message);

            throw new InvalidOperationException(
                $"HKS bildirimi {message.SaleTransactionId} üretilemedi: {createResult.Error}");
        }

        var notification = createResult.Value;

        var sendResult = await _gateway.SendHksNotificationAsync(notification, ct);
        if (sendResult.IsFailure)
        {
            _logger.LogError(
                "HKS bildirimi gönderimi başarısız: Tenant={TenantId} Sale={SaleTransactionId} Hata={ErrorCode} — {ErrorMessage}.",
                message.TenantId,
                message.SaleTransactionId,
                sendResult.Error.Code,
                sendResult.Error.Message);

            throw new InvalidOperationException(
                $"HKS bildirimi {message.SaleTransactionId} gönderimi başarısız: {sendResult.Error}");
        }

        var notifyResult = notification.MarkNotified(sendResult.Value);
        if (notifyResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"HKS bildirimi {message.SaleTransactionId} gönderilemedi: {notifyResult.Error}");
        }

        _notifications.Add(notification);

        // HksNotified event'i SaveChanges içinde tenant'lı olarak outbox'a atomik yazılır (docs/04 §10).
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "HKS bildirimi gönderildi: Tenant={TenantId} Sale={SaleTransactionId} Ref={ReferenceNumber} " +
            "Brüt={GrossAmount} Rüsum={MarketFeeAmount}.",
            message.TenantId,
            message.SaleTransactionId,
            notification.ReferenceNumber,
            notification.GrossAmount,
            notification.MarketFeeAmount);
    }

    /// <summary>
    /// Kayıt tutmayan müstahsil için e-MM üretir (BK-4). Profil senkronlanmadıysa e-Fatura+HKS zaten
    /// kalıcılaştıktan sonra İSTİSNA fırlatılır (retry). e-MM'e YALNIZ stopaj + Bağ-Kur girer (BK-1).
    /// </summary>
    private async Task ProduceProducerReceiptAsync(SaleCompleted message, CancellationToken ct)
    {
        // --- Idempotency: bu satış için e-MM zaten üretilmişse hiçbir şey yapma (docs/04 §5) ---
        var existing = await _receipts.GetBySaleTransactionIdAsync(message.SaleTransactionId, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "e-MM zaten mevcut, atlandı (idempotent): Tenant={TenantId} Sale={SaleTransactionId}.",
                message.TenantId,
                message.SaleTransactionId);
            return;
        }

        // --- e-MM gerekliliği (BK-4): müstahsil kayıt tutmuyorsa üret ---
        var profile = await _profiles.GetByProducerAsync(message.ProducerPartyId, ct);

        if (profile is null)
        {
            // Profil henüz senkronlanmamış: KeepsRecords bilinmiyor → e-MM gerekliliğine KARAR VERİLEMEZ.
            // e-Fatura + HKS bu noktada zaten üretilip kalıcılaştı (yukarıda); İSTİSNA fırla → MassTransit
            // retry/error queue devreye girer; retry'da e-Fatura+HKS idempotent atlanır, e-MM yeniden
            // denenir. Ack silent belge kaybı olurdu (docs/02 §3.5: kayıt tutmayan müstahsil için e-MM).
            _logger.LogWarning(
                "Müstahsil profili yok (KeepsRecords bilinmiyor); e-MM üretilemedi, yeniden denenecek: " +
                "Tenant={TenantId} Sale={SaleTransactionId} Producer={ProducerPartyId}.",
                message.TenantId,
                message.SaleTransactionId,
                message.ProducerPartyId);

            throw new InvalidOperationException(
                $"e-MM {message.SaleTransactionId} için müstahsil profili henüz senkronlanmadı " +
                $"(Producer={message.ProducerPartyId}); KeepsRecords bilinmiyor, yeniden denenecek.");
        }

        if (profile.KeepsRecords)
        {
            // Kayıt tutan müstahsile e-MM düzenlenmez (kendi belgesini keser — BK-4).
            _logger.LogInformation(
                "Müstahsil kayıt tutuyor; e-MM üretilmedi (BK-4): Tenant={TenantId} Sale={SaleTransactionId} Producer={ProducerPartyId}.",
                message.TenantId,
                message.SaleTransactionId,
                message.ProducerPartyId);
            return;
        }

        // --- e-MM üret (stopaj + Bağ-Kur; komisyon/rüsum/KDV GİRMEZ — BK-1) ---
        var createResult = ProducerReceipt.Create(
            message.TenantId,
            message.SaleTransactionId,
            message.ProducerPartyId,
            message.BuyerPartyId,
            message.SoldAt,
            message.GrossAmount,
            message.AgriWithholdingAmount,
            message.FarmerSskAmount);

        if (createResult.IsFailure)
        {
            // Bozuk/kötücül SaleCompleted (ör. kesintiler brütü aşıyor). Yut ma → istisna (retry, BK-1).
            _logger.LogError(
                "e-MM üretilemedi (reddedildi): Tenant={TenantId} Sale={SaleTransactionId} Hata={ErrorCode} — {ErrorMessage}.",
                message.TenantId,
                message.SaleTransactionId,
                createResult.Error.Code,
                createResult.Error.Message);

            throw new InvalidOperationException(
                $"e-MM {message.SaleTransactionId} üretilemedi: {createResult.Error}");
        }

        var receipt = createResult.Value;

        // --- GİB'e gönder (bu slice STUB; ADR-007 gerçek entegrasyon sonraki slice) ---
        var sendResult = await _gateway.SendProducerReceiptAsync(receipt, ct);
        if (sendResult.IsFailure)
        {
            // Gönderim başarısız: belgeyi kalıcılaştırıp Issued yapma; istisna → retry/error queue
            // (docs/04 §10). Yutulan Result yok.
            _logger.LogError(
                "e-MM gönderimi başarısız: Tenant={TenantId} Sale={SaleTransactionId} Hata={ErrorCode} — {ErrorMessage}.",
                message.TenantId,
                message.SaleTransactionId,
                sendResult.Error.Code,
                sendResult.Error.Message);

            throw new InvalidOperationException(
                $"e-MM {message.SaleTransactionId} gönderimi başarısız: {sendResult.Error}");
        }

        var issueResult = receipt.MarkIssued(sendResult.Value);
        if (issueResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"e-MM {message.SaleTransactionId} kesilemedi: {issueResult.Error}");
        }

        _receipts.Add(receipt);

        // ProducerReceiptIssued event'i SaveChanges içinde tenant'lı olarak outbox'a atomik yazılır
        // (docs/04 §10); handler doğrudan yayın yapmaz (docs/07 §5).
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "e-MM kesildi: Tenant={TenantId} Sale={SaleTransactionId} Producer={ProducerPartyId} " +
            "MakbuzNo={ReceiptNumber} Net={NetPayable}.",
            message.TenantId,
            message.SaleTransactionId,
            message.ProducerPartyId,
            receipt.ReceiptNumber,
            receipt.NetPayable);
    }
}

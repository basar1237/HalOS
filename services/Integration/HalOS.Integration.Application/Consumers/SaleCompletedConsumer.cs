using HalOS.BuildingBlocks.Contracts;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Integration.Application.Consumers;

/// <summary>
/// Sales servisinden gelen <see cref="SaleCompleted"/>'i tüketip gerekliyse e-Müstahsil Makbuzu (e-MM)
/// üretir (docs/02 §3.5 / §6: SaleCompleted → e-Belge e-MM; docs/03 M7 / BK-4; docs/04 ADR-007/§10).
///
/// <b>e-MM üretim kararı (BK-4):</b> e-MM YALNIZ kayıt TUTMAYAN müstahsil için üretilir. Müstahsilin
/// <c>KeepsRecords</c> bilgisi Party event'inden gelen <see cref="Domain.ReadModels.ProducerTaxProfile"/>
/// okuma modelinden okunur:
/// <list type="bullet">
///   <item>Profil yok (henüz senkronlanmamış) → KARAR VERİLEMEZ; e-MM üretmek/atlamak yerine
///     İSTİSNA fırlatılır → MassTransit retry/error queue devreye girer (docs/04 §10 en-az-bir-kez,
///     sıra garantisi YOK). Profil senkronu satıştan sonra gelirse retry ile yakalanır. e-MM yasal
///     zorunlu belgedir (VUK); mesajı ack'leyip düşürmek belge kaybı olur (docs/02 §3.5 değişmez).</item>
///   <item>KeepsRecords = true → e-MM ÜRETME (kayıt tutan müstahsile e-MM düzenlenmez; info log).</item>
///   <item>KeepsRecords = false → e-MM ÜRET (idempotent, SaleTransactionId ile).</item>
/// </list>
///
/// <b>Kesinti kırılımı:</b> e-MM'e YALNIZ stopaj + çiftçi Bağ-Kur girer (komisyon/rüsum/KDV girmez —
/// docs/02 §1.2/§1.3, BK-1). Bu iki tutar YENİDEN HESAPLANMAZ; <see cref="SaleCompleted"/>'ın taşıdığı
/// <c>AgriWithholdingAmount</c> + <c>FarmerSskAmount</c> kullanılır (Sales tek gerçeklik kaynağıdır;
/// docs/04 §10 event-taşımalı). SaleCompleted.TotalDeductions komisyon+rüsum de içerdiğinden e-MM için
/// YETERSİZDİR — bu yüzden Sales publish tarafında kırılım eklendi (bkz. Contracts/SaleCompleted, notes).
///
/// <b>Idempotency</b> (docs/04 §5): bir satış tenant içinde en fazla BİR e-MM üretir; aynı SaleCompleted
/// tekrar gelse (broker retry) ikinci kez belge oluşmaz. <b>Tenant</b>: mesajdan
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
    private readonly IProducerTaxProfileReader _profiles;
    private readonly IEDocumentGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaleCompletedConsumer> _logger;

    public SaleCompletedConsumer(
        IProducerReceiptRepository receipts,
        IProducerTaxProfileReader profiles,
        IEDocumentGateway gateway,
        IUnitOfWork unitOfWork,
        ILogger<SaleCompletedConsumer> logger)
    {
        _receipts = receipts;
        _profiles = profiles;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SaleCompleted> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

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
            // Mesajı NORMAL dönüşle ack'lemek YASAK: e-MM yasal zorunlu belgedir (VUK) ve reissue MEVCUT
            // bir belgeyi yeniden gönderir — HİÇ oluşturulmamış belgeyi kuramaz; ack silent belge kaybıdır
            // (docs/02 §3.5: her başarılı satış → kayıt tutmayan müstahsil için e-MM). Party.Register
            // müstahsil için ProducerWithholdingProfileChanged'i her zaman raise ettiğinden profil normalde
            // satıştan önce gelir; ancak sıra garanti değildir (docs/04 §10). Bu yüzden İSTİSNA fırla →
            // MassTransit retry/error queue devreye girer; profil senkronu geciktiyse yeniden teslimatta
            // yakalanır (KeepsRecords null durumu diğer başarısızlıklarla aynı şekilde ele alınır, BK-4).
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

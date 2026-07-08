using HalOS.BuildingBlocks.Contracts;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Integration.Application.Consumers;

/// <summary>
/// Sales servisinden gelen <see cref="ConsignmentReceived"/>'i tüketip mal geliş partisinin HER kalemi
/// için künye (<see cref="ProductPassport"/>) üretir (docs/02 §6 event katalog satır 229:
/// ConsignmentReceived → e-Belge/künye; docs/03 M8 / BK-4; docs/04 ADR-007/§10).
///
/// <b>Künye ürün-bazlıdır</b> (docs/02 §3.5): HKS 19-haneli kod üretim yeri/tür/miktar/üretici/sertifika
/// kodlar → parti kalemi başına BİR künye. Kalem bilgisi event'in <see cref="ConsignmentReceived.Items"/>
/// listesinden gelir (yeniden sorgu yok — docs/07 §5). <b>Idempotency</b>: kalem başına
/// (ConsignmentItemId) en fazla bir künye; aynı event tekrar gelse (broker retry) çift künye oluşmaz.
///
/// <b>Tenant</b>: mesajdan (<see cref="ITenantScopedEvent"/>) <c>TenantConsumeFilter</c> ile ambient
/// tenant'a set edilir; DbContext global query filter DOĞRU tenant'ta çalışır (docs/07 §6 / BK-8).
/// El-yapımı outbox korunur (ProductPassportIssued'lar SaveChanges içinde outbox'a). <b>Yutulan Result
/// yok</b>: gateway/domain IsFailure → SaveChanges'ten ÖNCE istisna → MassTransit retry/error queue
/// (docs/04 §10). e-MM/e-Fatura/HKS consumer deseniyle birebir.
/// </summary>
public sealed class ConsignmentReceivedConsumer : IConsumer<ConsignmentReceived>
{
    private readonly IProductPassportRepository _passports;
    private readonly IEDocumentGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConsignmentReceivedConsumer> _logger;

    public ConsignmentReceivedConsumer(
        IProductPassportRepository passports,
        IEDocumentGateway gateway,
        IUnitOfWork unitOfWork,
        ILogger<ConsignmentReceivedConsumer> logger)
    {
        _passports = passports;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ConsignmentReceived> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var producedCount = 0;

        foreach (var item in message.Items)
        {
            // Idempotency (docs/04 §5): bu kalem için künye zaten üretilmişse atla.
            var existing = await _passports.GetByConsignmentItemIdAsync(item.ConsignmentItemId, ct);
            if (existing is not null)
            {
                continue;
            }

            var createResult = ProductPassport.Create(
                message.TenantId,
                message.ConsignmentId,
                item.ConsignmentItemId,
                item.ProductId,
                message.ProducerPartyId,
                item.Quantity,
                item.UnitCode,
                message.ReceivedAt);

            if (createResult.IsFailure)
            {
                _logger.LogError(
                    "Künye üretilemedi (reddedildi): Tenant={TenantId} Consignment={ConsignmentId} Item={ConsignmentItemId} Hata={ErrorCode} — {ErrorMessage}.",
                    message.TenantId,
                    message.ConsignmentId,
                    item.ConsignmentItemId,
                    createResult.Error.Code,
                    createResult.Error.Message);

                throw new InvalidOperationException(
                    $"Künye üretilemedi (Consignment={message.ConsignmentId}, Item={item.ConsignmentItemId}): {createResult.Error}");
            }

            var passport = createResult.Value;

            // HKS 19-haneli kod (bu slice STUB; ADR-007 gerçek entegrasyon sonraki slice).
            var codeResult = await _gateway.GenerateProductPassportAsync(passport, ct);
            if (codeResult.IsFailure)
            {
                _logger.LogError(
                    "Künye HKS kodu üretimi başarısız: Tenant={TenantId} Item={ConsignmentItemId} Hata={ErrorCode} — {ErrorMessage}.",
                    message.TenantId,
                    item.ConsignmentItemId,
                    codeResult.Error.Code,
                    codeResult.Error.Message);

                throw new InvalidOperationException(
                    $"Künye HKS kodu üretilemedi (Item={item.ConsignmentItemId}): {codeResult.Error}");
            }

            var issueResult = passport.MarkIssued(codeResult.Value);
            if (issueResult.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Künye tescillenemedi (Item={item.ConsignmentItemId}): {issueResult.Error}");
            }

            _passports.Add(passport);
            producedCount++;
        }

        if (producedCount > 0)
        {
            // ProductPassportIssued event'leri SaveChanges içinde tenant'lı olarak outbox'a atomik yazılır
            // (docs/04 §10); consumer doğrudan yayın yapmaz (docs/07 §5).
            await _unitOfWork.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Künye üretimi tamamlandı: Tenant={TenantId} Consignment={ConsignmentId} ÜretilenKünye={ProducedCount} ToplamKalem={ItemCount}.",
            message.TenantId,
            message.ConsignmentId,
            producedCount,
            message.Items.Count);
    }
}

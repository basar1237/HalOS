using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.Aggregates;
using Microsoft.Extensions.Logging;

namespace HalOS.Integration.Infrastructure.Gateways;

/// <summary>
/// <see cref="IEDocumentGateway"/>'in bu slice'a özgü STUB uygulaması (docs/04 ADR-007). Gerçek GİB
/// e-Fatura + HKS + e-MM sandbox entegrasyonu SONRAKİ slice'ta gelir; şu an dış G/Ç yapılmaz, sahte
/// ama benzersiz belge/referans numaraları üretilip başarı döndürülür. Böylece e-Belge akışı
/// (SaleCompleted → belge → outbox event) uçtan uca çalışır ve gerçek entegrasyonda yalnız bu adaptör
/// değiştirilir (bağımlılığın tersine çevrilmesi — Application soyutlamaya bağlıdır, uygulamaya değil).
///
/// Gerçek entegrasyonda gönderim outbox tetikli/asenkron olacaktır (docs/07 §5: consumer içinde
/// doğrudan dış HTTP çağrısı yapılmaz). Bu STUB senkron ve yan-etkisiz olduğundan consumer güvenle
/// çağırır (notes: gerçek gönderim ayrı outbox worker'ına taşınacak).
/// </summary>
internal sealed class StubEDocumentGateway : IEDocumentGateway
{
    private readonly ILogger<StubEDocumentGateway> _logger;

    public StubEDocumentGateway(ILogger<StubEDocumentGateway> logger)
    {
        _logger = logger;
    }

    public Task<Result<string>> SendProducerReceiptAsync(ProducerReceipt receipt, CancellationToken cancellationToken = default)
    {
        // Sahte makbuz numarası: "EMM-<yyyyMMdd>-<8 hex>". Gerçek entegrasyonda GİB döner.
        var receiptNumber = $"EMM-{receipt.IssueDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        _logger.LogInformation(
            "STUB e-MM gönderimi: Tenant={TenantId} Sale={SaleTransactionId} MakbuzNo={ReceiptNumber} (gerçek GİB entegrasyonu sonraki slice).",
            receipt.TenantId,
            receipt.SaleTransactionId,
            receiptNumber);

        // Implicit dönüşüm (string → Result<string>.Success) — BuildingBlocks.Domain deseni.
        return Task.FromResult<Result<string>>(receiptNumber);
    }

    public Task<Result<string>> SendInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        // Sahte fatura numarası: "EFA-<yyyyMMdd>-<8 hex>". Gerçek entegrasyonda GİB döner.
        var invoiceNumber = $"EFA-{invoice.IssueDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        _logger.LogInformation(
            "STUB e-Fatura gönderimi: Tenant={TenantId} Sale={SaleTransactionId} FaturaNo={InvoiceNumber} (gerçek GİB entegrasyonu sonraki slice).",
            invoice.TenantId,
            invoice.SaleTransactionId,
            invoiceNumber);

        return Task.FromResult<Result<string>>(invoiceNumber);
    }

    public Task<Result<string>> SendHksNotificationAsync(HksNotification notification, CancellationToken cancellationToken = default)
    {
        // Sahte HKS referans numarası: "HKS-<yyyyMMdd>-<8 hex>". Gerçek entegrasyonda HKS döner.
        var referenceNumber = $"HKS-{notification.NotifiedDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        _logger.LogInformation(
            "STUB HKS bildirimi: Tenant={TenantId} Sale={SaleTransactionId} Ref={ReferenceNumber} (gerçek HKS entegrasyonu sonraki slice).",
            notification.TenantId,
            notification.SaleTransactionId,
            referenceNumber);

        return Task.FromResult<Result<string>>(referenceNumber);
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.PendingDocuments;

/// <summary>
/// Bekleyen e-belge sayılarını üç belge deposundan (e-Fatura/e-MM/HKS) toplayan query handler.
/// Her repo AsNoTracking + tenant filtreli sayım yapar (BK-8). Yeni tablo YOK.
/// </summary>
internal sealed class PendingDocumentsHandler
    : IQueryHandler<PendingDocumentsQuery, PendingDocumentsDto>
{
    private readonly IInvoiceRepository _invoices;
    private readonly IProducerReceiptRepository _receipts;
    private readonly IHksNotificationRepository _hks;

    public PendingDocumentsHandler(
        IInvoiceRepository invoices,
        IProducerReceiptRepository receipts,
        IHksNotificationRepository hks)
    {
        _invoices = invoices;
        _receipts = receipts;
        _hks = hks;
    }

    public async Task<Result<PendingDocumentsDto>> Handle(
        PendingDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var invoices = await _invoices.CountPendingAsync(cancellationToken);
        var receipts = await _receipts.CountPendingAsync(cancellationToken);
        var hks = await _hks.CountPendingAsync(cancellationToken);

        return Result.Success(
            new PendingDocumentsDto(invoices, receipts, hks, invoices + receipts + hks));
    }
}

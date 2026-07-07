using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;

namespace HalOS.Integration.Application.Features.ReissueInvoice;

/// <summary>
/// Başarısız/taslak e-Fatura'yı GİB'e yeniden gönderip keser (docs/03 §5 red yönetimi; docs/04 ADR-007).
/// Zaten kesilmiş (Issued) belge idempotent olarak başarıyla döner (MarkIssued no-op). Gönderim
/// başarısızsa Result.Failure (API 4xx/5xx) — yutulan Result yok. Tenant JWT'den (BK-8). e-MM
/// ReissueProducerReceiptHandler deseniyle birebir.
/// </summary>
internal sealed class ReissueInvoiceHandler : ICommandHandler<ReissueInvoiceCommand>
{
    private readonly IInvoiceRepository _invoices;
    private readonly IEDocumentGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;

    public ReissueInvoiceHandler(
        IInvoiceRepository invoices,
        IEDocumentGateway gateway,
        IUnitOfWork unitOfWork)
    {
        _invoices = invoices;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReissueInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure(Invoice.InvoiceErrors.NotFound);
        }

        // Zaten kesilmişse tekrar gönderme (idempotent başarı — MarkIssued no-op olur).
        if (invoice.Status == InvoiceStatus.Issued)
        {
            return Result.Success();
        }

        var sendResult = await _gateway.SendInvoiceAsync(invoice, cancellationToken);
        if (sendResult.IsFailure)
        {
            // Yeniden gönderim de başarısız: belgeyi Failed işaretleyip kaydet (durum izlenebilir),
            // sonucu hata olarak döndür. Yutulan Result yok.
            invoice.MarkFailed();
            _invoices.Update(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(sendResult.Error);
        }

        var issueResult = invoice.MarkIssued(sendResult.Value);
        if (issueResult.IsFailure)
        {
            return issueResult;
        }

        _invoices.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;

namespace HalOS.Integration.Application.Features.ReissueProducerReceipt;

/// <summary>
/// Başarısız/taslak e-MM'i GİB'e yeniden gönderip keser (docs/03 §5 red yönetimi; docs/04 ADR-007).
/// Zaten kesilmiş (Issued) belge idempotent olarak başarıyla döner (MarkIssued no-op). Gönderim
/// başarısızsa Result.Failure (API 4xx/5xx) — yutulan Result yok. Tenant JWT'den (BK-8).
/// </summary>
internal sealed class ReissueProducerReceiptHandler : ICommandHandler<ReissueProducerReceiptCommand>
{
    private readonly IProducerReceiptRepository _receipts;
    private readonly IEDocumentGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;

    public ReissueProducerReceiptHandler(
        IProducerReceiptRepository receipts,
        IEDocumentGateway gateway,
        IUnitOfWork unitOfWork)
    {
        _receipts = receipts;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReissueProducerReceiptCommand request, CancellationToken cancellationToken)
    {
        var receipt = await _receipts.GetByIdAsync(request.ReceiptId, cancellationToken);
        if (receipt is null)
        {
            return Result.Failure(ProducerReceipt.ProducerReceiptErrors.NotFound);
        }

        // Zaten kesilmişse tekrar gönderme (idempotent başarı — MarkIssued no-op olur).
        if (receipt.Status == ProducerReceiptStatus.Issued)
        {
            return Result.Success();
        }

        var sendResult = await _gateway.SendProducerReceiptAsync(receipt, cancellationToken);
        if (sendResult.IsFailure)
        {
            // Yeniden gönderim de başarısız: belgeyi Failed işaretleyip kaydet (durum izlenebilir),
            // sonucu hata olarak döndür. Yutulan Result yok.
            receipt.MarkFailed();
            _receipts.Update(receipt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(sendResult.Error);
        }

        var issueResult = receipt.MarkIssued(sendResult.Value);
        if (issueResult.IsFailure)
        {
            return issueResult;
        }

        _receipts.Update(receipt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

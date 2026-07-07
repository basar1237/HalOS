using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;

namespace HalOS.Integration.Application.Features.ReissueHksNotification;

/// <summary>
/// Başarısız/taslak HKS bildirimini yeniden gönderir (docs/03 §5 red yönetimi; docs/04 ADR-007). Zaten
/// gönderilmiş (Notified) belge idempotent olarak başarıyla döner (MarkNotified no-op). Gönderim
/// başarısızsa Result.Failure (API 4xx/5xx) — yutulan Result yok. Tenant JWT'den (BK-8). e-MM
/// ReissueProducerReceiptHandler deseniyle birebir.
/// </summary>
internal sealed class ReissueHksNotificationHandler : ICommandHandler<ReissueHksNotificationCommand>
{
    private readonly IHksNotificationRepository _notifications;
    private readonly IEDocumentGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;

    public ReissueHksNotificationHandler(
        IHksNotificationRepository notifications,
        IEDocumentGateway gateway,
        IUnitOfWork unitOfWork)
    {
        _notifications = notifications;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReissueHksNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notifications.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
        {
            return Result.Failure(HksNotification.HksNotificationErrors.NotFound);
        }

        // Zaten gönderilmişse tekrar gönderme (idempotent başarı — MarkNotified no-op olur).
        if (notification.Status == HksNotificationStatus.Notified)
        {
            return Result.Success();
        }

        var sendResult = await _gateway.SendHksNotificationAsync(notification, cancellationToken);
        if (sendResult.IsFailure)
        {
            // Yeniden gönderim de başarısız: belgeyi Failed işaretleyip kaydet (durum izlenebilir),
            // sonucu hata olarak döndür. Yutulan Result yok.
            notification.MarkFailed();
            _notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(sendResult.Error);
        }

        var notifyResult = notification.MarkNotified(sendResult.Value);
        if (notifyResult.IsFailure)
        {
            return notifyResult;
        }

        _notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

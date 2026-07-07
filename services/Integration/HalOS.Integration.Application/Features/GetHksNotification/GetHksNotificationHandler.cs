using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;

namespace HalOS.Integration.Application.Features.GetHksNotification;

/// <summary>HKS bildirimini kimliğiyle getiren query handler (docs/03 M8). Tenant filtreli (BK-8).</summary>
internal sealed class GetHksNotificationHandler : IQueryHandler<GetHksNotificationQuery, HksNotificationDto>
{
    private readonly IHksNotificationRepository _notifications;

    public GetHksNotificationHandler(IHksNotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result<HksNotificationDto>> Handle(GetHksNotificationQuery request, CancellationToken cancellationToken)
    {
        var notification = await _notifications.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
        {
            return Result.Failure<HksNotificationDto>(HksNotification.HksNotificationErrors.NotFound);
        }

        return HksNotificationDto.FromDomain(notification);
    }
}

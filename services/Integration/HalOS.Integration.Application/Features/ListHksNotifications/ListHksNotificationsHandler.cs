using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.ListHksNotifications;

/// <summary>Sayfalanmış HKS bildirimi listesini döndüren query handler (docs/03 M8). Tenant filtreli (BK-8).</summary>
internal sealed class ListHksNotificationsHandler
    : IQueryHandler<ListHksNotificationsQuery, PagedResult<HksNotificationDto>>
{
    private readonly IHksNotificationRepository _notifications;

    public ListHksNotificationsHandler(IHksNotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result<PagedResult<HksNotificationDto>>> Handle(
        ListHksNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _notifications.ListAsync(request.Page, request.PageSize, cancellationToken);

        var items = page.Items.Select(HksNotificationDto.FromDomain).ToList();

        return new PagedResult<HksNotificationDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

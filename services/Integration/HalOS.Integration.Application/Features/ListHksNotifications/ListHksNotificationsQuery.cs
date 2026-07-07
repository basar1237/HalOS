using HalOS.BuildingBlocks.Application;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.ListHksNotifications;

/// <summary>
/// Tenant filtreli, sayfalanmış HKS bildirimi listesi (docs/03 §5 e-Belge Merkezi; docs/03 M8).
/// </summary>
public sealed record ListHksNotificationsQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<HksNotificationDto>>;

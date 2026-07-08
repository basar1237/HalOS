using HalOS.Notification.Application.Abstractions;
using HalOS.Notification.Domain;
using Microsoft.AspNetCore.SignalR;

namespace HalOS.Notification.Api.Realtime;

/// <summary>
/// <see cref="IDashboardBroadcaster"/>'ın SignalR uygulaması (docs/06 S2.2). Bildirimi
/// <see cref="IHubContext{DashboardHub}"/> üzerinden YALNIZ <c>tenant-{id}</c> grubuna
/// (<see cref="DashboardGroups.ForTenant"/>) <c>"notify"</c> metoduyla iletir. Grup, hub'ta JWT
/// tenant'ından türetilen grupla birebir aynıdır; dolayısıyla yayın yalnız ilgili tenant'ın bağlı
/// istemcilerine ulaşır (BK-8, çapraz-tenant sızıntısı YASAK).
/// </summary>
public sealed class SignalRDashboardBroadcaster : IDashboardBroadcaster
{
    private readonly IHubContext<DashboardHub> _hubContext;

    public SignalRDashboardBroadcaster(IHubContext<DashboardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public Task BroadcastAsync(Guid tenantId, DashboardNotification notification, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(DashboardGroups.ForTenant(tenantId))
            .SendAsync(DashboardHub.ClientNotifyMethod, notification, cancellationToken);
    }
}

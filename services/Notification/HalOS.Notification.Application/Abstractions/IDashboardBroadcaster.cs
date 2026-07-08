using HalOS.Notification.Domain;

namespace HalOS.Notification.Application.Abstractions;

/// <summary>
/// Canlı dashboard yayın soyutlaması (docs/06 S2.2). Consumer bunu çağırır; somut uygulama
/// (Api katmanındaki <c>SignalRDashboardBroadcaster</c>) bildirimi SignalR üzerinden YALNIZ ilgili
/// tenant grubuna (<c>tenant-{id}</c>) iletir (BK-8). Bu soyutlama sayesinde Application katmanı
/// SignalR'a (ASP.NET Core) bağlanmaz ve testler <c>IHubContext</c> yerine bunu mock'lar.
/// </summary>
public interface IDashboardBroadcaster
{
    /// <summary>
    /// Bildirimi <paramref name="tenantId"/>'nin canlı dashboard grubuna yayınlar. Yalnız o tenant'ın
    /// bağlı istemcileri alır; başka tenant ASLA almaz (çapraz-tenant sızıntısı YASAK, BK-8).
    /// </summary>
    /// <param name="tenantId">Yayının hedeflendiği kiracı (event'in taşıdığı tenant).</param>
    /// <param name="notification">Yayınlanacak bildirim.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    Task BroadcastAsync(Guid tenantId, DashboardNotification notification, CancellationToken cancellationToken = default);
}

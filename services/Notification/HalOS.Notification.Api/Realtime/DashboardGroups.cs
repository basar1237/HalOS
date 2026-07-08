namespace HalOS.Notification.Api.Realtime;

/// <summary>
/// SignalR tenant grup adlandırmasının TEK doğruluk kaynağı. Hem <see cref="DashboardHub"/> (bağlanan
/// istemciyi gruba ekler) hem <see cref="SignalRDashboardBroadcaster"/> (o gruba yayınlar) bu metodu
/// kullanır — böylece katılım ve yayın grup adı ASLA uyuşmazlığa düşmez. Grup adı sunucuda JWT
/// tenant'ından türetilir; istemci grup adını kendisi SEÇEMEZ (BK-8, çapraz-tenant sızıntısı YASAK).
/// </summary>
public static class DashboardGroups
{
    /// <summary>Verilen tenant için canlı dashboard SignalR grup adını (<c>tenant-{id}</c>) döndürür.</summary>
    public static string ForTenant(Guid tenantId) => $"tenant-{tenantId}";
}

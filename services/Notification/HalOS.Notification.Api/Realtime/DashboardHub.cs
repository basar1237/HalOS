using System.Security.Claims;
using HalOS.Notification.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HalOS.Notification.Api.Realtime;

/// <summary>
/// Canlı dashboard SignalR hub'ı (docs/06 S2.2). Patron/yönetici ekranı buraya bağlanır ve
/// <c>SaleCompleted</c> gibi olaylardan türeyen bildirimleri gerçek zamanlı alır (<c>"notify"</c>
/// istemci metodu).
///
/// TENANT İZOLASYONU (BK-8): <see cref="OnConnectedAsync"/>, bağlanan kullanıcının JWT
/// <c>tenant_id</c> claim'inden tenant'ı okur ve bağlantıyı YALNIZ <c>tenant-{id}</c> grubuna
/// katar. Broadcast yalnız bu gruba gider; başka tenant'ın istemcisi ASLA almaz. Grup adını sunucu
/// belirler — istemci kendi grubunu SEÇEMEZ. Tenant claim'i yoksa/geçersizse bağlantı reddedilir
/// (<see cref="HubConnectionContext.Abort"/>).
/// </summary>
[Authorize]
public sealed class DashboardHub : Hub
{
    /// <summary>İstemcinin dinlediği yayın metodu adı (sözleşme; broadcaster ile aynı olmalı).</summary>
    public const string ClientNotifyMethod = "notify";

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        var tenantValue = Context.User?.FindFirstValue(HalOSClaimTypes.TenantId);

        // Tenant çözülemiyorsa bağlantıyı reddet — tenant grubu olmadan yayın alamaz ve
        // izolasyon garanti edilemez (BK-8).
        if (!Guid.TryParse(tenantValue, out var tenantId) || tenantId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroups.ForTenant(tenantId));
        await base.OnConnectedAsync();
    }
}

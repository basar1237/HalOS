using FluentAssertions;
using HalOS.Notification.Api.Realtime;
using HalOS.Notification.Domain;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace HalOS.Notification.Tests;

/// <summary>
/// <see cref="SignalRDashboardBroadcaster"/>: bildirimi YALNIZ <c>tenant-{id}</c> grubuna, <c>"notify"</c>
/// metoduyla iletir (BK-8). Grup adı hub'ın katıldığı grupla birebir aynı olmalı (DashboardGroups tek
/// kaynak). IHubContext/IClientProxy mock'lanır.
/// </summary>
public sealed class SignalRDashboardBroadcasterTests
{
    private static DashboardNotification Sample(Guid tenantId) =>
        new(
            Type: "sale.completed",
            TenantId: tenantId,
            Title: "Yeni satış",
            Message: "Yeni satış: 1,100.50 TL net, 1,250.50 brüt",
            Payload: new Dictionary<string, object?> { ["k"] = "v" },
            OccurredOnUtc: DateTime.UtcNow);

    [Fact]
    public async Task BroadcastAsync_SendsNotifyToTenantGroupOnly()
    {
        var tenantId = Guid.NewGuid();
        var clientProxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group($"tenant-{tenantId}")).Returns(clientProxy.Object);
        var hubContext = new Mock<IHubContext<DashboardHub>>();
        hubContext.SetupGet(h => h.Clients).Returns(clients.Object);

        var broadcaster = new SignalRDashboardBroadcaster(hubContext.Object);
        var notification = Sample(tenantId);

        await broadcaster.BroadcastAsync(tenantId, notification);

        // Yalnız ilgili tenant grubu çözüldü (başka grup çağrılmadı, BK-8).
        clients.Verify(c => c.Group($"tenant-{tenantId}"), Times.Once);
        clients.Verify(c => c.Group(It.Is<string>(s => s != $"tenant-{tenantId}")), Times.Never);

        // "notify" metoduyla, bildirim argüman olarak gönderildi (SendCoreAsync ile doğrulanır).
        clientProxy.Verify(
            p => p.SendCoreAsync(
                DashboardHub.ClientNotifyMethod,
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], notification)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void BroadcasterGroupName_MatchesHubJoinConvention()
    {
        // Regresyon: broadcaster ve hub AYNI grup adını kullanmalı; aksi halde yayın kimseye ulaşmaz.
        var tenantId = Guid.NewGuid();
        DashboardGroups.ForTenant(tenantId).Should().Be($"tenant-{tenantId}");
    }
}

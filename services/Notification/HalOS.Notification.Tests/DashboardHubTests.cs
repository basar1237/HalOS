using System.Security.Claims;
using FluentAssertions;
using HalOS.Notification.Api.Realtime;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace HalOS.Notification.Tests;

/// <summary>
/// <see cref="DashboardHub"/> grup-katılım mantığı (BK-8): bağlanan istemci, JWT <c>tenant_id</c>
/// claim'inden türeyen <c>tenant-{id}</c> grubuna katılır; tenant claim'i yoksa bağlantı reddedilir
/// (Abort). İstemci grup adını SEÇEMEZ — sunucu belirler. Hafif birim testi: HubCallerContext/Groups
/// mock'lanır (docs/06 S2.2).
/// </summary>
public sealed class DashboardHubTests
{
    private const string TenantClaim = "tenant_id";

    private static DashboardHub HubWith(ClaimsPrincipal? user, Mock<IGroupManager> groups, Mock<HubCallerContext> context)
    {
        context.SetupGet(c => c.User).Returns(user);
        context.SetupGet(c => c.ConnectionId).Returns("conn-1");
        return new DashboardHub
        {
            Context = context.Object,
            Groups = groups.Object
        };
    }

    private static ClaimsPrincipal UserWithTenant(Guid tenantId) =>
        new(new ClaimsIdentity(new[] { new Claim(TenantClaim, tenantId.ToString()) }, "test"));

    [Fact]
    public async Task OnConnected_WithTenantClaim_JoinsTenantGroup()
    {
        var tenantId = Guid.NewGuid();
        var groups = new Mock<IGroupManager>();
        var context = new Mock<HubCallerContext>();
        var hub = HubWith(UserWithTenant(tenantId), groups, context);

        await hub.OnConnectedAsync();

        // Grup adı "tenant-{id}" ve sunucuca JWT'den belirlenir (istemci seçemez, BK-8).
        groups.Verify(
            g => g.AddToGroupAsync("conn-1", $"tenant-{tenantId}", It.IsAny<CancellationToken>()),
            Times.Once);
        context.Verify(c => c.Abort(), Times.Never);
    }

    [Fact]
    public async Task OnConnected_WithoutTenantClaim_AbortsAndDoesNotJoin()
    {
        var groups = new Mock<IGroupManager>();
        var context = new Mock<HubCallerContext>();
        var hub = HubWith(new ClaimsPrincipal(new ClaimsIdentity()), groups, context);

        await hub.OnConnectedAsync();

        context.Verify(c => c.Abort(), Times.Once);
        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnConnected_WithEmptyTenant_Aborts()
    {
        var groups = new Mock<IGroupManager>();
        var context = new Mock<HubCallerContext>();
        var hub = HubWith(UserWithTenant(Guid.Empty), groups, context);

        await hub.OnConnectedAsync();

        context.Verify(c => c.Abort(), Times.Once);
        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001", "tenant-00000000-0000-0000-0000-000000000001")]
    public void DashboardGroups_ForTenant_IsTenantPrefixedId(string tenant, string expected)
    {
        DashboardGroups.ForTenant(Guid.Parse(tenant)).Should().Be(expected);
    }
}

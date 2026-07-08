using FluentAssertions;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Notification.Application.Abstractions;
using HalOS.Notification.Application.Consumers;
using HalOS.Notification.Domain;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HalOS.Notification.Tests;

/// <summary>
/// <see cref="SaleCompletedConsumer"/> davranışı: IDashboardBroadcaster MOCK'u ile (SignalR'a bağlanmadan)
/// test edilir. Kapsam (docs/06 S2.2, BK-8):
/// <list type="bullet">
///   <item>Consume → BroadcastAsync DOĞRU tenantId + beklenen içerikle (net/brüt özet) çağrılır.</item>
///   <item>Farklı tenant event'i farklı tenant'a yayınlanır (çapraz-tenant izolasyon).</item>
///   <item>Bildirim tipi "sale.completed" ve payload satış kimliğini/tutarları taşır.</item>
/// </list>
/// </summary>
public sealed class SaleCompletedConsumerTests
{
    private static ConsumeContext<T> ContextFor<T>(T message)
        where T : class
    {
        var mock = new Mock<ConsumeContext<T>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    private static SaleCompleted SampleSale(Guid tenantId, Guid saleId, decimal gross = 1250.50m, decimal net = 1100.50m) =>
        new(
            SaleTransactionId: saleId,
            TenantId: tenantId,
            BuyerPartyId: Guid.NewGuid(),
            ProducerPartyId: Guid.NewGuid(),
            SoldAt: new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc),
            GrossAmount: gross,
            CommissionAmount: 100m,
            CommissionVatAmount: 20m,
            AgriWithholdingAmount: 25m,
            FarmerSskAmount: 12.5m,
            MarketFeeAmount: 12.5m,
            TotalDeductions: 150m,
            NetAmount: net,
            SettlementDueDate: new DateTime(2026, 7, 28),
            Lines: Array.Empty<SaleCompletedLine>(),
            OccurredOnUtc: new DateTime(2026, 7, 6, 10, 0, 5, DateTimeKind.Utc));

    [Fact]
    public async Task Consume_Broadcasts_ToEventTenant_WithExpectedContent()
    {
        var broadcaster = new Mock<IDashboardBroadcaster>();
        DashboardNotification? captured = null;
        Guid capturedTenant = Guid.Empty;
        broadcaster
            .Setup(b => b.BroadcastAsync(It.IsAny<Guid>(), It.IsAny<DashboardNotification>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, DashboardNotification, CancellationToken>((t, n, _) => { capturedTenant = t; captured = n; })
            .Returns(Task.CompletedTask);

        var consumer = new SaleCompletedConsumer(broadcaster.Object, NullLogger<SaleCompletedConsumer>.Instance);
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        await consumer.Consume(ContextFor(SampleSale(tenantId, saleId)));

        // Tenant, event'ten (ITenantScopedEvent) alınır — DOĞRU tenant grubuna yayınlanır (BK-8).
        capturedTenant.Should().Be(tenantId);
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.Type.Should().Be(NotificationTypes.SaleCompleted);
        captured.Type.Should().Be("sale.completed");
        captured.Title.Should().Be("Yeni satış");
        // Net + brüt özet (docs/02 §6 patrona canlı özet).
        captured.Message.Should().Be("Yeni satış: 1,100.50 TL net, 1,250.50 brüt");
        captured.Payload.Should().ContainKey("saleTransactionId").WhoseValue.Should().Be(saleId);
        captured.Payload.Should().ContainKey("netAmount").WhoseValue.Should().Be(1100.50m);
        captured.Payload.Should().ContainKey("grossAmount").WhoseValue.Should().Be(1250.50m);
        captured.OccurredOnUtc.Should().Be(new DateTime(2026, 7, 6, 10, 0, 5, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Consume_CalledExactlyOnce_PerEvent()
    {
        var broadcaster = new Mock<IDashboardBroadcaster>();
        broadcaster
            .Setup(b => b.BroadcastAsync(It.IsAny<Guid>(), It.IsAny<DashboardNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var consumer = new SaleCompletedConsumer(broadcaster.Object, NullLogger<SaleCompletedConsumer>.Instance);
        var tenantId = Guid.NewGuid();

        await consumer.Consume(ContextFor(SampleSale(tenantId, Guid.NewGuid())));

        broadcaster.Verify(
            b => b.BroadcastAsync(tenantId, It.IsAny<DashboardNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_DifferentTenants_BroadcastToDifferentTenants_Isolation_BK8()
    {
        // BK-8: tenant A'nın satışı yalnız A'ya, tenant B'nin satışı yalnız B'ye yayınlanır.
        var broadcaster = new Mock<IDashboardBroadcaster>();
        broadcaster
            .Setup(b => b.BroadcastAsync(It.IsAny<Guid>(), It.IsAny<DashboardNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var consumer = new SaleCompletedConsumer(broadcaster.Object, NullLogger<SaleCompletedConsumer>.Instance);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await consumer.Consume(ContextFor(SampleSale(tenantA, Guid.NewGuid())));
        await consumer.Consume(ContextFor(SampleSale(tenantB, Guid.NewGuid())));

        // Her tenant kendi bildirimini alır; A'ya yapılan yayın B tenant'ıyla ASLA çağrılmaz.
        broadcaster.Verify(
            b => b.BroadcastAsync(tenantA, It.Is<DashboardNotification>(n => n.TenantId == tenantA), It.IsAny<CancellationToken>()),
            Times.Once);
        broadcaster.Verify(
            b => b.BroadcastAsync(tenantB, It.Is<DashboardNotification>(n => n.TenantId == tenantB), It.IsAny<CancellationToken>()),
            Times.Once);
        // A'ya B içeriği (veya tersi) SIZMAZ.
        broadcaster.Verify(
            b => b.BroadcastAsync(tenantA, It.Is<DashboardNotification>(n => n.TenantId == tenantB), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

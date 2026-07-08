using System.Globalization;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Notification.Application.Abstractions;
using HalOS.Notification.Domain;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Notification.Application.Consumers;

/// <summary>
/// Sales servisinden gelen <see cref="SaleCompleted"/>'ı tüketip patronun canlı dashboard'una özet
/// bildirim yayınlar (docs/02: <c>SaleCompleted</c> → Bildirim → patrona canlı özet; docs/06 S2.2).
/// Notification servisi salt tüketici→broadcast'tir: DB'ye YAZMAZ, kaynak servisin (Sales) DB'sine
/// DOKUNMAZ, event YAYMAZ, consumer içinde HTTP/dış çağrı YOK (docs/07 §5).
///
/// Tenant mesajdan gelir (<see cref="ITenantScopedEvent"/>) ve yayın YALNIZ o tenant'ın grubuna
/// (<c>tenant-{id}</c>) gider — başka tenant'ın dashboard'u bu satışı ASLA görmez (BK-8).
/// </summary>
public sealed class SaleCompletedConsumer : IConsumer<SaleCompleted>
{
    private readonly IDashboardBroadcaster _broadcaster;
    private readonly ILogger<SaleCompletedConsumer> _logger;

    public SaleCompletedConsumer(IDashboardBroadcaster broadcaster, ILogger<SaleCompletedConsumer> logger)
    {
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SaleCompleted> context)
    {
        var message = context.Message;

        // Patrona okunur özet: net + brüt tutar (docs/02 §6 canlı özet). Kültür-bağımsız formatla
        // taşınır; istemci gösterimi kendi yerelleştirmesini uygular.
        var netText = message.NetAmount.ToString("N2", CultureInfo.InvariantCulture);
        var grossText = message.GrossAmount.ToString("N2", CultureInfo.InvariantCulture);
        var messageText = $"Yeni satış: {netText} TL net, {grossText} brüt";

        var notification = new DashboardNotification(
            Type: NotificationTypes.SaleCompleted,
            TenantId: message.TenantId,
            Title: "Yeni satış",
            Message: messageText,
            Payload: new Dictionary<string, object?>
            {
                ["saleTransactionId"] = message.SaleTransactionId,
                ["grossAmount"] = message.GrossAmount,
                ["netAmount"] = message.NetAmount,
                ["soldAt"] = message.SoldAt
            },
            OccurredOnUtc: message.OccurredOnUtc);

        await _broadcaster.BroadcastAsync(message.TenantId, notification, context.CancellationToken);

        _logger.LogInformation(
            "Canlı dashboard bildirimi yayınlandı: Tenant={TenantId} Sale={SaleTransactionId} Net={NetAmount}.",
            message.TenantId,
            message.SaleTransactionId,
            message.NetAmount);
    }
}

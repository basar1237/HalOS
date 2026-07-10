using System.Globalization;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Notification.Application.Abstractions;
using HalOS.Notification.Domain;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Notification.Application.Consumers;

/// <summary>
/// ColdChain servisinden gelen <see cref="TemperatureThresholdBreached"/>'ı tüketip patronun canlı
/// dashboard'una SOĞUK ZİNCİR ALARMI yayınlar (docs/04 §6: eşik aşımı → anlık bildirim; docs/06 S3.1).
/// Notification salt tüketici→broadcast'tir: DB'ye YAZMAZ, kaynak DB'ye DOKUNMAZ, event YAYMAZ (docs/07 §5).
///
/// Tenant mesajdan gelir (<see cref="ITenantScopedEvent"/>) ve yayın YALNIZ o tenant'ın grubuna gider (BK-8).
/// <see cref="SaleCompletedConsumer"/> deseniyle birebir.
/// </summary>
public sealed class TemperatureThresholdBreachedConsumer : IConsumer<TemperatureThresholdBreached>
{
    private readonly IDashboardBroadcaster _broadcaster;
    private readonly ILogger<TemperatureThresholdBreachedConsumer> _logger;

    public TemperatureThresholdBreachedConsumer(
        IDashboardBroadcaster broadcaster,
        ILogger<TemperatureThresholdBreachedConsumer> logger)
    {
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TemperatureThresholdBreached> context)
    {
        var m = context.Message;

        var tempText = m.TemperatureC.ToString("N1", CultureInfo.InvariantCulture);
        var direction = m.AboveMax ? "üst eşik aşıldı (çok sıcak)" : "alt eşik altına inildi (çok soğuk)";
        var messageText = $"{m.UnitName}: {tempText}°C — {direction}";

        var notification = new DashboardNotification(
            Type: NotificationTypes.TemperatureThresholdBreached,
            TenantId: m.TenantId,
            Title: "Soğuk zincir alarmı",
            Message: messageText,
            Payload: new Dictionary<string, object?>
            {
                ["coldStorageUnitId"] = m.ColdStorageUnitId,
                ["unitName"] = m.UnitName,
                ["temperatureC"] = m.TemperatureC,
                ["minTempC"] = m.MinTempC,
                ["maxTempC"] = m.MaxTempC,
                ["aboveMax"] = m.AboveMax,
                ["occurredAt"] = m.OccurredAt
            },
            OccurredOnUtc: m.OccurredOnUtc);

        await _broadcaster.BroadcastAsync(m.TenantId, notification, context.CancellationToken);

        _logger.LogWarning(
            "Soğuk zincir alarmı yayınlandı: Tenant={TenantId} Unit={ColdStorageUnitId} Temp={TemperatureC} AboveMax={AboveMax}.",
            m.TenantId,
            m.ColdStorageUnitId,
            m.TemperatureC,
            m.AboveMax);
    }
}

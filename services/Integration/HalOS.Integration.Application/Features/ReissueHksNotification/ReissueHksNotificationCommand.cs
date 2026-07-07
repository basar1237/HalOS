using HalOS.BuildingBlocks.Application;

namespace HalOS.Integration.Application.Features.ReissueHksNotification;

/// <summary>
/// Başarısız (Failed) veya taslak (Draft) bir HKS bildirimini yeniden gönderir (docs/03 §5 e-Belge
/// Merkezi "red yönetimi"; docs/03 BK-4 belge reddi). Yetki: Muhasebe/Yönetici/Patron (docs/03 §3).
/// e-MM ReissueProducerReceipt deseniyle birebir.
/// </summary>
public sealed record ReissueHksNotificationCommand(Guid NotificationId) : ICommand;

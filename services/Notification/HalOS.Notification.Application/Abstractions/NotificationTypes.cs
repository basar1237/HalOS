namespace HalOS.Notification.Application.Abstractions;

/// <summary>
/// Kanonik dashboard bildirim tür kodları (<c>DashboardNotification.Type</c>). İstemci bu sabitlere
/// göre ikon/renk seçer ve filtreler; consumer'lar aynı sabiti kullanır (tek doğruluk kaynağı).
/// </summary>
public static class NotificationTypes
{
    /// <summary>Bir satış tamamlandığında yayınlanan bildirim türü (kaynak: <c>SaleCompleted</c>).</summary>
    public const string SaleCompleted = "sale.completed";
}

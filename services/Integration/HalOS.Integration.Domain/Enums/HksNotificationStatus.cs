namespace HalOS.Integration.Domain.Enums;

/// <summary>
/// HKS Bildirimi (<c>HksNotification</c>) yaşam döngüsü durumu (docs/02 §3.5 <c>LegalDocument</c> alt
/// tipi; docs/03 M8 / BK-4). Enum kolonu metin (HasConversion&lt;string&gt; — docs/07). HKS web
/// servisine gönderim ADR-007/ADR-010 gereği retry + outbox ile yapılır; bu slice'ta gönderim STUB'tur
/// (gerçek HKS sandbox entegrasyonu sonraki slice). e-MM (<c>ProducerReceiptStatus</c>) deseniyle birebir.
/// </summary>
public enum HksNotificationStatus
{
    /// <summary>Bildirim oluşturuldu ama henüz HKS'e gönderilmedi (taslak).</summary>
    Draft = 1,

    /// <summary>Bildirim başarıyla gönderildi; HKS referans numarası atandı.</summary>
    Notified = 2,

    /// <summary>Gönderim/red nedeniyle başarısız — kullanıcı uyarılır, yeniden denenebilir (docs/03 BK-4).</summary>
    Failed = 3,

    /// <summary>Bildirim iptal edildi (satış iptali; denetim izi korunur — BK-9).</summary>
    Cancelled = 4
}

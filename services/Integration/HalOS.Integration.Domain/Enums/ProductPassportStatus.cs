namespace HalOS.Integration.Domain.Enums;

/// <summary>
/// Künye (<c>ProductPassport</c>) yaşam döngüsü durumu (docs/02 §3.5 <c>ProductPassport</c>; docs/03 M8 /
/// BK-4). Enum kolonu metin (HasConversion&lt;string&gt; — docs/07). HKS'e künye tescili (19-haneli kod
/// üretimi) ADR-007/ADR-010 gereği retry + outbox ile yapılır; bu slice'ta üretim STUB'tur (gerçek HKS
/// sandbox entegrasyonu sonraki slice). e-MM (<c>ProducerReceiptStatus</c>) / HKS
/// (<c>HksNotificationStatus</c>) deseniyle birebir.
/// </summary>
public enum ProductPassportStatus
{
    /// <summary>Künye oluşturuldu ama HKS 19-haneli kodu henüz atanmadı (taslak).</summary>
    Draft = 1,

    /// <summary>Künye başarıyla tescillendi; HKS 19-haneli künye kodu atandı (QR ile sorgulanır).</summary>
    Issued = 2,

    /// <summary>Kod üretimi/tescil başarısız — kullanıcı uyarılır, yeniden denenebilir (docs/03 BK-4).</summary>
    Failed = 3
}

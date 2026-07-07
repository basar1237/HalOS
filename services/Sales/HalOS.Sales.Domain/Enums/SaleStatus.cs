namespace HalOS.Sales.Domain.Enums;

/// <summary>
/// Satış kaydının durumu (docs/05 §3.5 <c>sale_transaction.status</c>: draft/completed/cancelled).
/// Kod adı İngilizce (docs/07 §3); kullanıcıya görünen ad Türkçe.
/// </summary>
public enum SaleStatus
{
    /// <summary>Taslak — satır girişi/düzenleme aşaması; henüz kesinti hesaplanmadı.</summary>
    Draft = 1,

    /// <summary>Tamamlandı — kesinti/hakediş hesaplandı; SaleCompleted yayınlandı (BK-1).</summary>
    Completed = 2,

    /// <summary>İptal edildi — ters kayıt/flag ile (Completed satış SİLİNMEZ, BK-9).</summary>
    Cancelled = 3
}

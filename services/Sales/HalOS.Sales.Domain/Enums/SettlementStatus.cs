namespace HalOS.Sales.Domain.Enums;

/// <summary>
/// Müstahsile hakediş durumu (docs/05 §3.5 <c>settlement.status</c>: pending/scheduled/paid).
/// Kod adı İngilizce (docs/07 §3). Ödeme Finance servisinde yapılır; Sales yalnızca hakedişi
/// üretir ve <c>due_date</c>'i 15 iş günü olarak hesaplar (BK-3).
/// </summary>
public enum SettlementStatus
{
    /// <summary>Bekliyor — hesaplandı, henüz ödeme planına alınmadı.</summary>
    Pending = 1,

    /// <summary>Planlandı — ödeme planına alındı (Finance).</summary>
    Scheduled = 2,

    /// <summary>Ödendi (Finance).</summary>
    Paid = 3
}

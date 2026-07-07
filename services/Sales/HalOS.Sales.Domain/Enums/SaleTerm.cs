namespace HalOS.Sales.Domain.Enums;

/// <summary>
/// Satışın ödeme vadesi türü (docs/03 §4 BK-3). Müstahsile ödeme planı buna göre kurulur:
/// normal (peşin) satışta <b>15 iş günü</b>, vadeli satışta <b>30 gün</b> içinde. Kod adı
/// İngilizce (docs/07 §3); kullanıcıya görünen ad Türkçe. Metin olarak saklanır (docs/05 §3.5).
/// </summary>
public enum SaleTerm
{
    /// <summary>Peşin/normal satış — ödeme 15 iş günü içinde (BK-3).</summary>
    Cash = 1,

    /// <summary>Vadeli satış — ödeme 30 gün içinde (BK-3).</summary>
    Deferred = 2
}

namespace HalOS.Sales.Domain.Enums;

/// <summary>
/// Mal geliş partisinin durumu (docs/05 §3.4 <c>consignment.status</c>). Kod adı İngilizce
/// (docs/07 §3); kullanıcıya görünen ad Türkçe.
/// </summary>
public enum ConsignmentStatus
{
    /// <summary>Kabul edildi; satışa hazır.</summary>
    Received = 1,

    /// <summary>Kısmen veya tamamen satışa konu oldu.</summary>
    InSale = 2,

    /// <summary>Kapatıldı (tüm mal satıldı/uzlaşıldı).</summary>
    Closed = 3
}

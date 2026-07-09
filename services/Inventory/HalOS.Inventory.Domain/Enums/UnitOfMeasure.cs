namespace HalOS.Inventory.Domain.Enums;

/// <summary>
/// Ölçü birimi (docs/02 §1.4 <c>UnitOfMeasure</c>; docs/05 §3.3). Kod adı İngilizce (docs/07 §3);
/// kullanıcıya görünen ad Türkçe. Değerler Sales.UnitOfMeasure ile AYNI (servisler-arası tutarlılık;
/// tip paylaşılmaz — her servis kendi domain enum'ına sahiptir, ortak sözleşme yalnız event'lerde).
/// Metin olarak saklanır (HasConversion&lt;string&gt; — docs/07 §3).
/// </summary>
public enum UnitOfMeasure
{
    /// <summary>Kasa.</summary>
    Crate = 1,

    /// <summary>Kilogram.</summary>
    Kilogram = 2,

    /// <summary>Çuval.</summary>
    Sack = 3,

    /// <summary>Adet.</summary>
    Piece = 4,

    /// <summary>Sandık.</summary>
    Box = 5
}

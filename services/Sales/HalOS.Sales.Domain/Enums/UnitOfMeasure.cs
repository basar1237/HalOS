namespace HalOS.Sales.Domain.Enums;

/// <summary>
/// Ölçü birimi (docs/02 §1.4 <c>UnitOfMeasure</c>; docs/05 §3.3 <c>unit_of_measure.code</c>).
/// Kod adı İngilizce (docs/07 §3); kullanıcıya görünen ad Türkçe. Kasa/Kg/Çuval/Adet/Sandık.
/// </summary>
public enum UnitOfMeasure
{
    /// <summary>Kasa — standart taşıma/sunum birimi (~5-20 kg).</summary>
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

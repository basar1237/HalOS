namespace HalOS.Integration.Domain.ValueObjects;

/// <summary>
/// Para hesaplama yardımcıları (docs/07 §4 / BK-2). Tüm parasal değerler <see cref="decimal"/>
/// (asla float/double). Yuvarlama YALNIZCA son adımda kuruşa (2 hane) ve banker's rounding
/// (<see cref="MidpointRounding.ToEven"/>) ile yapılır. Kural tek noktada toplanır ki tüm servis
/// aynı yuvarlamayı kullansın (Finance.Money / Sales.Money deseniyle birebir — docs/07 §10).
/// </summary>
public static class Money
{
    /// <summary>Kuruş ondalık basamak sayısı (2 hane).</summary>
    public const int Scale = 2;

    /// <summary>
    /// Bir tutarı kuruşa yuvarlar: <c>Math.Round(value, 2, MidpointRounding.ToEven)</c> (BK-2).
    /// </summary>
    public static decimal RoundToKurus(decimal value) =>
        Math.Round(value, Scale, MidpointRounding.ToEven);
}

namespace HalOS.Sales.Domain.ValueObjects;

/// <summary>
/// Para hesaplama yardımcıları (docs/07 §4 / BK-2). Tüm parasal değerler <see cref="decimal"/>
/// (asla float/double). Yuvarlama YALNIZCA son adımda kuruşa (2 hane) ve banker's rounding
/// (<see cref="MidpointRounding.ToEven"/>) ile yapılır. Hesap sırası sabit: yüzde uygula →
/// (topla) → yuvarla (docs/03 §4 BK-1/BK-2). Kural tek noktada toplanır ki tüm motor aynı
/// yuvarlamayı kullansın (docs/07 §10 sihirli sabit yasağı).
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

    /// <summary>
    /// Brüt üzerinden oranla kesinti tutarını hesaplar ve kuruşa yuvarlar (BK-1). Motor her
    /// kesintiyi <c>Round(gross * rate, 2, ToEven)</c> ile ürettiğinden bu tek metotta toplanır.
    /// </summary>
    public static decimal ApplyRate(decimal baseAmount, decimal rate) =>
        RoundToKurus(baseAmount * rate);
}

namespace HalOS.Sales.Application.Contracts;

/// <summary>
/// Satış trend raporu (docs/06 S2.2 "Rapor v2: trend") zaman kovası kırınım seviyesi. Tamamlanmış
/// satışlar SoldAt (UTC) tarihine göre bu granülariteye göre kovalanır.
/// </summary>
public enum TrendGranularity
{
    /// <summary>Günlük kova: her kova bir takvim günü (UTC gün başı).</summary>
    Day = 0,

    /// <summary>Haftalık kova: ISO-8601 hafta başı (Pazartesi 00:00, UTC).</summary>
    Week = 1,

    /// <summary>Aylık kova: ayın ilk günü (UTC ay başı).</summary>
    Month = 2
}

/// <summary>
/// Satış trend raporu okuma DTO'su (docs/06 S2.2). Verilen [FromUtc, ToUtc] aralığındaki TAMAMLANMIŞ
/// (Status=Completed) satışlar <see cref="Granularity"/> seviyesine göre zaman kovalarına gruplanır;
/// her kova için adet/brüt/komisyon/net döner (PeriodStart'a göre artan sıralı). Tutarlar decimal
/// (BK-2). Yeni tablo YOK — mevcut SaleTransaction/CommissionCalculation/Settlement verisi okunur.
/// </summary>
/// <param name="Granularity">Uygulanan kırınım seviyesi (Gün/Hafta/Ay).</param>
/// <param name="Buckets">Zaman kovaları (PeriodStart artan sıralı). Boş aralıkta boş liste.</param>
public sealed record SalesTrendReportDto(
    TrendGranularity Granularity,
    IReadOnlyList<SalesTrendBucketDto> Buckets);

/// <summary>
/// Satış trend kovası satırı (docs/06 S2.2). Bir zaman kovasındaki tamamlanmış satış toplamları.
/// Tutarlar decimal (BK-2).
/// </summary>
/// <param name="PeriodStart">Kovanın başlangıcı (UTC): gün başı / hafta başı (Pzt) / ay başı.</param>
/// <param name="Count">Kovadaki tamamlanmış satış adedi.</param>
/// <param name="Gross">Kovanın toplam brüt bedeli = Σ SaleTransaction.GrossAmount.</param>
/// <param name="Commission">Kovanın toplam komisyonu = Σ CommissionCalculation.CommissionAmount.</param>
/// <param name="Net">Kovanın toplam net hakedişi = Σ Settlement.NetAmount.</param>
public sealed record SalesTrendBucketDto(
    DateTime PeriodStart,
    long Count,
    decimal Gross,
    decimal Commission,
    decimal Net);

using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.Reports.SalesTrendReport;

/// <summary>
/// Satış trend raporu (docs/06 S2.2 "Rapor v2: trend"). Verilen tarih aralığındaki (SoldAt)
/// TAMAMLANMIŞ satışları zaman kovalarına (Gün/Hafta/Ay) gruplar; her kova için adet/brüt/komisyon/
/// net döner. SALT-OKUMA CQRS query — yeni tablo/servis yok; tenant global filter otomatik uygulanır
/// (BK-8).
/// </summary>
/// <param name="FromUtc">Aralık başlangıcı (dahil, UTC).</param>
/// <param name="ToUtc">
/// Aralık bitişi (UTC). Gün bazında DAHİL: bitiş gününün TÜM saatleri kapsanır (mevcut rapor
/// deseniyle tutarlı — üst sınır ertesi gün 00:00'a normalize edilir).
/// </param>
/// <param name="Granularity">Kova kırınım seviyesi (Gün/Hafta/Ay). Varsayılan Gün.</param>
public sealed record SalesTrendReportQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    TrendGranularity Granularity = TrendGranularity.Day) : IQuery<SalesTrendReportDto>;

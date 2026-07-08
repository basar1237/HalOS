using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.Reports.SalesSummaryReport;

/// <summary>
/// Satış özet raporu (docs/03 M10 "raporlar (okuma)"). Verilen tarih aralığındaki (SoldAt)
/// TAMAMLANMIŞ satışların adet/brüt/komisyon/kesinti/net toplamlarını döner. SALT-OKUMA CQRS
/// query — yeni tablo/servis yok; tenant global filter otomatik uygulanır (BK-8).
/// </summary>
/// <param name="FromUtc">Aralık başlangıcı (dahil, UTC).</param>
/// <param name="ToUtc">
/// Aralık bitişi (UTC). Gün bazında DAHİL: bitiş gününün TÜM saatleri kapsanır (örn. ToUtc=07.07
/// → 07.07 10:00 satışı sayılır). Saat bileşeni yok sayılır; sınır ertesi gün 00:00'a normalize edilir.
/// </param>
public sealed record SalesSummaryReportQuery(
    DateTime FromUtc,
    DateTime ToUtc) : IQuery<SalesSummaryReportDto>;

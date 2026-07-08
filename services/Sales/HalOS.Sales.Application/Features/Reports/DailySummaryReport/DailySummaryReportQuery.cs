using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.Reports.DailySummaryReport;

/// <summary>
/// Gün sonu özet raporu (docs/03 M10). Verilen günün (SoldAt tarihi, UTC) tamamlanmış satış
/// toplamları: adet/brüt/komisyon/net. SALT-OKUMA CQRS query — yeni tablo/servis yok; tenant
/// global filter otomatik uygulanır (BK-8).
/// </summary>
/// <param name="Day">Rapor günü; yalnız tarih kısmı (UTC) kullanılır.</param>
public sealed record DailySummaryReportQuery(DateTime Day) : IQuery<DailySummaryReportDto>;

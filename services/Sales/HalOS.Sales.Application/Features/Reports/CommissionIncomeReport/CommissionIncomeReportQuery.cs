using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.Reports.CommissionIncomeReport;

/// <summary>
/// Komisyon geliri raporu (docs/03 M10). Aralıktaki tamamlanmış satışlar için komisyon geliri =
/// Σ komisyon tutarı + Σ komisyon KDV'si; opsiyonel günlük kırılım. SALT-OKUMA CQRS query — yeni
/// tablo/servis yok; tenant global filter otomatik uygulanır (BK-8).
/// </summary>
/// <param name="FromUtc">Aralık başlangıcı (dahil, UTC).</param>
/// <param name="ToUtc">
/// Aralık bitişi (UTC). Gün bazında DAHİL: bitiş gününün TÜM saatleri kapsanır (örn. ToUtc=07.07
/// → 07.07 10:00 satışı sayılır). Saat bileşeni yok sayılır; sınır ertesi gün 00:00'a normalize edilir.
/// </param>
/// <param name="IncludeDailyBreakdown">true ise günlük kırılım (Daily) doldurulur.</param>
public sealed record CommissionIncomeReportQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    bool IncludeDailyBreakdown = false) : IQuery<CommissionIncomeReportDto>;

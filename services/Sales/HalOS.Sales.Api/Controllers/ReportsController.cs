using HalOS.Sales.Api.Authorization;
using HalOS.Sales.Application.Features.Reports.CommissionIncomeReport;
using HalOS.Sales.Application.Features.Reports.DailySummaryReport;
using HalOS.Sales.Application.Features.Reports.SalesSummaryReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Sales.Api.Controllers;

/// <summary>
/// Satış tarafı raporları (docs/03 M10 "raporlar (okuma)") — SALT-OKUMA CQRS query'leri. Tenant
/// JWT claim'inden çözülür ve global query filter'a taşınır (BK-8). Yalnız tamamlanmış satışlar
/// üzerinden agregasyon; yeni tablo/servis yok. Okuma yetkisi: Patron/Yönetici/Muhasebe
/// (<see cref="AuthorizationPolicies.SalesReportRead"/>).
/// </summary>
[ApiController]
[Route("reports")]
[Authorize(Policy = AuthorizationPolicies.SalesReportRead)]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Satış özet raporu: aralıktaki tamamlanmış satışların adet/brüt/komisyon/kesinti(KDV hariç)/net
    /// toplamları (docs/03 M10).
    /// </summary>
    [HttpGet("sales-summary")]
    public async Task<IActionResult> SalesSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SalesSummaryReportQuery(from, to), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Komisyon geliri raporu: aralıktaki Σ komisyon + Σ komisyon KDV'si; opsiyonel günlük kırılım
    /// (docs/03 M10).
    /// </summary>
    [HttpGet("commission-income")]
    public async Task<IActionResult> CommissionIncome(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] bool daily = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new CommissionIncomeReportQuery(from, to, daily), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Gün sonu özeti: verilen günün tamamlanmış satış toplamları (docs/03 M10).</summary>
    [HttpGet("daily")]
    public async Task<IActionResult> Daily(
        [FromQuery] DateTime day,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DailySummaryReportQuery(day), cancellationToken);
        return result.ToActionResult(this);
    }
}

using System.Globalization;
using HalOS.Sales.Api.Authorization;
using HalOS.Sales.Application.Contracts;
using HalOS.Sales.Application.Features.Reports.CommissionIncomeReport;
using HalOS.Sales.Application.Features.Reports.DailySummaryReport;
using HalOS.Sales.Application.Features.Reports.SalesSummaryReport;
using HalOS.Sales.Application.Features.Reports.SalesTrendReport;
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

    /// <summary>
    /// Satış trend raporu: aralıktaki tamamlanmış satışları zaman kovalarına (Gün/Hafta/Ay) gruplar;
    /// her kova için adet/brüt/komisyon/net (docs/06 S2.2 "Rapor v2: trend").
    /// </summary>
    [HttpGet("trend")]
    public async Task<IActionResult> Trend(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] TrendGranularity granularity = TrendGranularity.Day,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new SalesTrendReportQuery(from, to, granularity), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Satış özet raporunu CSV olarak dışa aktarır (docs/06 S2.2 "dışa aktarma"). Aynı query çalışır,
    /// tek satırlık özet CSV döner. RFC 4180 kaçışlı; sayılar InvariantCulture.
    /// </summary>
    [HttpGet("sales-summary.csv")]
    public async Task<IActionResult> SalesSummaryCsv(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SalesSummaryReportQuery(from, to), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        var dto = result.Value;
        var bytes = CsvWriter.WriteBytes(
            new[] { "adet", "brut", "komisyon", "kesinti", "net" },
            new[]
            {
                new[]
                {
                    CsvWriter.Number(dto.Count),
                    CsvWriter.Money(dto.TotalGross),
                    CsvWriter.Money(dto.TotalCommission),
                    CsvWriter.Money(dto.TotalDeductions),
                    CsvWriter.Money(dto.TotalNet)
                }
            });

        return CsvFile(bytes, $"satis-ozeti-{FileStamp(from)}-{FileStamp(to)}.csv");
    }

    /// <summary>
    /// Komisyon geliri raporunu CSV olarak dışa aktarır (docs/06 S2.2). Günlük kırılım (daily=true)
    /// istenirse her gün bir satır; aksi halde tek toplam satırı. RFC 4180 kaçışlı.
    /// </summary>
    [HttpGet("commission-income.csv")]
    public async Task<IActionResult> CommissionIncomeCsv(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] bool daily = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new CommissionIncomeReportQuery(from, to, daily), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        var dto = result.Value;
        var header = new[] { "tarih", "komisyon", "kdv", "gelir" };
        var rows = new List<IReadOnlyList<string>>();

        if (daily && dto.Daily.Count > 0)
        {
            foreach (var d in dto.Daily)
            {
                rows.Add(new[]
                {
                    CsvWriter.Date(d.Day),
                    CsvWriter.Money(d.Commission),
                    CsvWriter.Money(d.Vat),
                    CsvWriter.Money(d.Income)
                });
            }
        }
        else
        {
            rows.Add(new[]
            {
                "TOPLAM",
                CsvWriter.Money(dto.TotalCommission),
                CsvWriter.Money(dto.TotalVat),
                CsvWriter.Money(dto.TotalIncome)
            });
        }

        var bytes = CsvWriter.WriteBytes(header, rows);
        return CsvFile(bytes, $"komisyon-geliri-{FileStamp(from)}-{FileStamp(to)}.csv");
    }

    /// <summary>
    /// Satış trend raporunu CSV olarak dışa aktarır (docs/06 S2.2). Her zaman kovası bir satır.
    /// RFC 4180 kaçışlı; tarih/sayı InvariantCulture.
    /// </summary>
    [HttpGet("trend.csv")]
    public async Task<IActionResult> TrendCsv(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] TrendGranularity granularity = TrendGranularity.Day,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new SalesTrendReportQuery(from, to, granularity), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        var dto = result.Value;
        var rows = dto.Buckets.Select(b => (IReadOnlyList<string>)new[]
        {
            CsvWriter.Date(b.PeriodStart),
            CsvWriter.Number(b.Count),
            CsvWriter.Money(b.Gross),
            CsvWriter.Money(b.Commission),
            CsvWriter.Money(b.Net)
        });

        var bytes = CsvWriter.WriteBytes(
            new[] { "donem", "adet", "brut", "komisyon", "net" }, rows);

        return CsvFile(bytes, $"satis-trend-{granularity}-{FileStamp(from)}-{FileStamp(to)}.csv".ToLowerInvariant());
    }

    private FileContentResult CsvFile(byte[] content, string fileName) =>
        File(content, "text/csv", fileName);

    private static string FileStamp(DateTime value) =>
        value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
}

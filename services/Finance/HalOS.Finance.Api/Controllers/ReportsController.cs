using System.Globalization;
using HalOS.Finance.Api.Authorization;
using HalOS.Finance.Application.Features.Reports.CurrentAccountAgingReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Finance.Api.Controllers;

/// <summary>
/// Finans tarafı raporları (docs/03 M10 "raporlar (okuma)") — SALT-OKUMA CQRS query'leri. Tenant
/// JWT claim'inden çözülür ve global query filter'a taşınır (BK-8). Cari hareket verisi üzerinden
/// agregasyon; yeni tablo/servis yok. Okuma yetkisi: Patron/Yönetici/Muhasebe
/// (<see cref="AuthorizationPolicies.FinanceReportRead"/>). Sales ReportsController deseniyle birebir.
/// </summary>
[ApiController]
[Route("reports")]
[Authorize(Policy = AuthorizationPolicies.FinanceReportRead)]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Cari yaşlandırma raporu: müstahsil hakediş (Settlement) vadelerini referans tarihe göre gecikme
    /// yaşına göre kovalara (güncel / 0-15 / 16-30 / 31+ gün) böler; her kova için tutar + cari sayısı
    /// (docs/03 M10). <paramref name="asOf"/> verilmezse şu anki UTC zamanı kullanılır.
    /// </summary>
    [HttpGet("aging")]
    public async Task<IActionResult> Aging(
        [FromQuery] DateTime? asOf,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CurrentAccountAgingReportQuery(asOf ?? DateTime.UtcNow), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Cari yaşlandırma raporunu CSV olarak dışa aktarır (docs/06 S2.2 "dışa aktarma"). Aynı query
    /// çalışır; her yaşlandırma kovası bir satır (kova / tutar / cari sayısı) + toplam satırı. RFC 4180
    /// kaçışlı; tarih/sayı InvariantCulture. <paramref name="asOf"/> verilmezse şu anki UTC zamanı.
    /// </summary>
    [HttpGet("aging.csv")]
    public async Task<IActionResult> AgingCsv(
        [FromQuery] DateTime? asOf,
        CancellationToken cancellationToken)
    {
        var asOfUtc = asOf ?? DateTime.UtcNow;
        var result = await _sender.Send(
            new CurrentAccountAgingReportQuery(asOfUtc), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        var dto = result.Value;
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "Guncel", CsvWriter.Money(dto.Current.Amount), CsvWriter.Number(dto.Current.AccountCount) },
            new[] { "0-15 gun", CsvWriter.Money(dto.Days0To15.Amount), CsvWriter.Number(dto.Days0To15.AccountCount) },
            new[] { "16-30 gun", CsvWriter.Money(dto.Days16To30.Amount), CsvWriter.Number(dto.Days16To30.AccountCount) },
            new[] { "31+ gun", CsvWriter.Money(dto.Days31Plus.Amount), CsvWriter.Number(dto.Days31Plus.AccountCount) },
            new[] { "TOPLAM", CsvWriter.Money(dto.TotalAmount), CsvWriter.Number(dto.TotalAccountCount) }
        };

        var bytes = CsvWriter.WriteBytes(
            new[] { "kova", "tutar", "cari_sayisi" }, rows);

        var stamp = asOfUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return File(bytes, "text/csv", $"cari-yaslandirma-{stamp}.csv");
    }
}

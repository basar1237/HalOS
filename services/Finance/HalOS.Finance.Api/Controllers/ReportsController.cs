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
}

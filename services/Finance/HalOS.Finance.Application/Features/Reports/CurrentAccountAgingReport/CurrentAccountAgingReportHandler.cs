using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Contracts;

namespace HalOS.Finance.Application.Features.Reports.CurrentAccountAgingReport;

/// <summary>
/// Cari yaşlandırma raporu query handler (docs/03 M10). Agregasyonu repository'nin AsNoTracking
/// okuma metoduna delege eder (tenant filtreli, yalnız vade taşıyan Settlement hareketleri).
/// Tutarlar decimal (BK-2). Sales rapor handler deseniyle birebir.
/// </summary>
internal sealed class CurrentAccountAgingReportHandler
    : IQueryHandler<CurrentAccountAgingReportQuery, CurrentAccountAgingReportDto>
{
    private readonly ICurrentAccountRepository _accounts;

    public CurrentAccountAgingReportHandler(ICurrentAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<Result<CurrentAccountAgingReportDto>> Handle(
        CurrentAccountAgingReportQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _accounts.GetCurrentAccountAgingAsync(request.AsOfUtc, cancellationToken);
        return dto;
    }
}

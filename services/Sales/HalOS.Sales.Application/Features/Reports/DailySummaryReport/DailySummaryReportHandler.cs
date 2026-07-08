using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.Reports.DailySummaryReport;

/// <summary>
/// Gün sonu özet raporu query handler (docs/03 M10). Agregasyonu repository'nin AsNoTracking okuma
/// metoduna delege eder (tenant filtreli, yalnız Completed, verilen günün [00:00, 24:00) aralığı).
/// Tutarlar decimal (BK-2).
/// </summary>
internal sealed class DailySummaryReportHandler
    : IQueryHandler<DailySummaryReportQuery, DailySummaryReportDto>
{
    private readonly ISaleTransactionRepository _sales;

    public DailySummaryReportHandler(ISaleTransactionRepository sales)
    {
        _sales = sales;
    }

    public async Task<Result<DailySummaryReportDto>> Handle(
        DailySummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _sales.GetDailySummaryAsync(request.Day, cancellationToken);
        return dto;
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.Reports.SalesSummaryReport;

/// <summary>
/// Satış özet raporu query handler (docs/03 M10). Agregasyonu repository'nin AsNoTracking okuma
/// metoduna delege eder (tenant filtreli, yalnız Completed). Tutarlar decimal (BK-2).
/// </summary>
internal sealed class SalesSummaryReportHandler
    : IQueryHandler<SalesSummaryReportQuery, SalesSummaryReportDto>
{
    private readonly ISaleTransactionRepository _sales;

    public SalesSummaryReportHandler(ISaleTransactionRepository sales)
    {
        _sales = sales;
    }

    public async Task<Result<SalesSummaryReportDto>> Handle(
        SalesSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _sales.GetSalesSummaryAsync(request.FromUtc, request.ToUtc, cancellationToken);
        return dto;
    }
}

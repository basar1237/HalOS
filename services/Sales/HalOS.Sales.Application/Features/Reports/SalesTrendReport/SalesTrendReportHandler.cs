using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.Reports.SalesTrendReport;

/// <summary>
/// Satış trend raporu query handler (docs/06 S2.2). Agregasyonu repository'nin AsNoTracking okuma
/// metoduna delege eder (tenant filtreli, yalnız Completed). Tutarlar decimal (BK-2). Mevcut Sales
/// rapor handler deseniyle birebir.
/// </summary>
internal sealed class SalesTrendReportHandler
    : IQueryHandler<SalesTrendReportQuery, SalesTrendReportDto>
{
    private readonly ISaleTransactionRepository _sales;

    public SalesTrendReportHandler(ISaleTransactionRepository sales)
    {
        _sales = sales;
    }

    public async Task<Result<SalesTrendReportDto>> Handle(
        SalesTrendReportQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _sales.GetSalesTrendAsync(
            request.FromUtc,
            request.ToUtc,
            request.Granularity,
            cancellationToken);

        return dto;
    }
}

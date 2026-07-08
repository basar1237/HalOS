using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.Reports.CommissionIncomeReport;

/// <summary>
/// Komisyon geliri raporu query handler (docs/03 M10). Agregasyonu repository'nin AsNoTracking
/// okuma metoduna delege eder (tenant filtreli, yalnız Completed). Tutarlar decimal (BK-2).
/// </summary>
internal sealed class CommissionIncomeReportHandler
    : IQueryHandler<CommissionIncomeReportQuery, CommissionIncomeReportDto>
{
    private readonly ISaleTransactionRepository _sales;

    public CommissionIncomeReportHandler(ISaleTransactionRepository sales)
    {
        _sales = sales;
    }

    public async Task<Result<CommissionIncomeReportDto>> Handle(
        CommissionIncomeReportQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _sales.GetCommissionIncomeAsync(
            request.FromUtc,
            request.ToUtc,
            request.IncludeDailyBreakdown,
            cancellationToken);

        return dto;
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.Reports.SpoilageAnalysisReport;

/// <summary>
/// Detaylı fire analizi raporu query handler (docs/06 S2.1). Agregasyonu repository'nin AsNoTracking
/// okuma metoduna delege eder (Kind-bazlı; tenant filtreli, BK-8). Miktarlar decimal (BK-2). Finance
/// CurrentAccountAgingReportHandler deseniyle birebir.
/// </summary>
internal sealed class SpoilageAnalysisReportHandler
    : IQueryHandler<SpoilageAnalysisReportQuery, SpoilageAnalysisReportDto>
{
    private readonly IStockItemRepository _stockItems;

    public SpoilageAnalysisReportHandler(IStockItemRepository stockItems)
    {
        _stockItems = stockItems;
    }

    public async Task<Result<SpoilageAnalysisReportDto>> Handle(
        SpoilageAnalysisReportQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _stockItems.GetSpoilageAnalysisAsync(request.FromUtc, request.ToUtc, cancellationToken);
        return dto;
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.Reports.DashboardSummary;

/// <summary>
/// Dashboard satış-tarafı özeti query handler (docs/02 §5). Bugünkü mal geliş adedini
/// (Consignment) ve toplam bekleyen hakedişi (Settlement) ilgili repository'lerin AsNoTracking
/// okuma metotlarından toplar. Tenant filtreli (BK-8).
/// </summary>
internal sealed class DashboardSummaryHandler
    : IQueryHandler<DashboardSummaryQuery, SalesDashboardDto>
{
    private readonly IConsignmentRepository _consignments;
    private readonly ISaleTransactionRepository _sales;

    public DashboardSummaryHandler(
        IConsignmentRepository consignments,
        ISaleTransactionRepository sales)
    {
        _consignments = consignments;
        _sales = sales;
    }

    public async Task<Result<SalesDashboardDto>> Handle(
        DashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var todayConsignments = await _consignments.CountReceivedOnAsync(request.Day, cancellationToken);
        var pendingSettlement = await _sales.GetPendingSettlementTotalAsync(cancellationToken);

        return Result.Success(new SalesDashboardDto(todayConsignments, pendingSettlement));
    }
}

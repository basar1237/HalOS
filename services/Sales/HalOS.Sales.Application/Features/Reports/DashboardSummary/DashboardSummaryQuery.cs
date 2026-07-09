using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.Reports.DashboardSummary;

/// <summary>
/// Kontrol paneli satış-tarafı özeti (docs/02 §5): verilen gün için mal geliş adedi + toplam
/// bekleyen hakediş. SALT-OKUMA CQRS; tenant global filter otomatik (BK-8).
/// </summary>
/// <param name="Day">"Bugünkü mal geliş" için referans gün (ReceivedAt tarihi, UTC).</param>
public sealed record DashboardSummaryQuery(DateTime Day) : IQuery<SalesDashboardDto>;

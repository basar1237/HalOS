using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.ListSales;

/// <summary>
/// Tenant filtreli, sold_at'e göre azalan sıralı sayfalanmış satış listesi (docs/03 M4;
/// docs/05 §6 (tenant_id, sold_at)). Opsiyonel tarih aralığı.
/// </summary>
public sealed record ListSalesQuery(
    int Page = 1,
    int PageSize = 20,
    DateTime? From = null,
    DateTime? To = null) : IQuery<PagedResult<SaleDto>>;

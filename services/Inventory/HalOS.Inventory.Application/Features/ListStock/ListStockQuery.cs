using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.ListStock;

/// <summary>Sayfalanmış stok kalemi listesi (kalan özetli) query (docs/02 §115). Tenant filtreli (BK-8).</summary>
public sealed record ListStockQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<StockItemDto>>;

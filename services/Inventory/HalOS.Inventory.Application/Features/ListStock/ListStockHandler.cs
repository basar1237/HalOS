using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.ListStock;

/// <summary>Sayfalanmış stok kalemi listesi query handler (docs/02 §115). Tenant filtreli (BK-8).</summary>
internal sealed class ListStockHandler : IQueryHandler<ListStockQuery, PagedResult<StockItemDto>>
{
    private readonly IStockItemRepository _stockItems;

    public ListStockHandler(IStockItemRepository stockItems)
    {
        _stockItems = stockItems;
    }

    public async Task<Result<PagedResult<StockItemDto>>> Handle(
        ListStockQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _stockItems.ListAsync(request.Page, request.PageSize, cancellationToken);

        var dto = new PagedResult<StockItemDto>(
            page.Items.Select(StockItemDto.FromDomain).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);

        return dto;
    }
}

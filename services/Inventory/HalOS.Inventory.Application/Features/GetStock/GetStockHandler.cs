using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.GetStock;

/// <summary>Bir ürünün stok kalemini getiren query handler (docs/02 §115). Tenant filtreli (BK-8).</summary>
internal sealed class GetStockHandler : IQueryHandler<GetStockQuery, StockItemDto>
{
    private readonly IStockItemRepository _stockItems;

    public GetStockHandler(IStockItemRepository stockItems)
    {
        _stockItems = stockItems;
    }

    public async Task<Result<StockItemDto>> Handle(GetStockQuery request, CancellationToken cancellationToken)
    {
        var stockItem = await _stockItems.GetByProductIdAsync(request.ProductId, cancellationToken);
        if (stockItem is null)
        {
            return Result.Failure<StockItemDto>(StockItemErrors.NotFound);
        }

        return StockItemDto.FromDomain(stockItem);
    }
}

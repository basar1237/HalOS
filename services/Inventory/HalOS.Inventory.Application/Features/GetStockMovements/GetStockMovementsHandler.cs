using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.GetStockMovements;

/// <summary>Stok hareket dökümü query handler (docs/02 §115). Tenant filtreli (BK-8).</summary>
internal sealed class GetStockMovementsHandler : IQueryHandler<GetStockMovementsQuery, StockMovementsDto>
{
    private readonly IStockItemRepository _stockItems;

    public GetStockMovementsHandler(IStockItemRepository stockItems)
    {
        _stockItems = stockItems;
    }

    public async Task<Result<StockMovementsDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var stockItem = await _stockItems.GetByProductIdAsync(request.ProductId, cancellationToken);
        if (stockItem is null)
        {
            return Result.Failure<StockMovementsDto>(StockItemErrors.NotFound);
        }

        return StockMovementsDto.FromDomain(stockItem);
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.GetStockMovements;

/// <summary>
/// Bir ürünün VARSAYILAN depodaki stok hareket dökümü query handler (docs/02 §115; docs/06 S2.1).
/// Olay-güdümlü giriş/çıkış varsayılan depoya yazıldığından döküm de varsayılan depoyu hedefler.
/// Tenant filtreli (BK-8).
/// </summary>
internal sealed class GetStockMovementsHandler : IQueryHandler<GetStockMovementsQuery, StockMovementsDto>
{
    private readonly IStockItemRepository _stockItems;
    private readonly IWarehouseRepository _warehouses;

    public GetStockMovementsHandler(IStockItemRepository stockItems, IWarehouseRepository warehouses)
    {
        _stockItems = stockItems;
        _warehouses = warehouses;
    }

    public async Task<Result<StockMovementsDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouses.GetDefaultAsync(cancellationToken);
        if (warehouse is null)
        {
            return Result.Failure<StockMovementsDto>(StockItemErrors.NotFound);
        }

        var stockItem = await _stockItems.GetByWarehouseAndProductAsync(
            warehouse.Id, request.ProductId, cancellationToken);
        if (stockItem is null)
        {
            return Result.Failure<StockMovementsDto>(StockItemErrors.NotFound);
        }

        return StockMovementsDto.FromDomain(stockItem);
    }
}

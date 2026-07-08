using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.GetStock;

/// <summary>
/// Bir ürünün VARSAYILAN depodaki stok kalemini getiren query handler (docs/02 §115; docs/06 S2.1
/// depo lokasyonu). Olay-güdümlü giriş/çıkış varsayılan depoya yazıldığından okuma da varsayılan
/// depoyu hedefler. Tenant filtreli (BK-8).
/// </summary>
internal sealed class GetStockHandler : IQueryHandler<GetStockQuery, StockItemDto>
{
    private readonly IStockItemRepository _stockItems;
    private readonly IWarehouseRepository _warehouses;

    public GetStockHandler(IStockItemRepository stockItems, IWarehouseRepository warehouses)
    {
        _stockItems = stockItems;
        _warehouses = warehouses;
    }

    public async Task<Result<StockItemDto>> Handle(GetStockQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouses.GetDefaultAsync(cancellationToken);
        if (warehouse is null)
        {
            return Result.Failure<StockItemDto>(StockItemErrors.NotFound);
        }

        var stockItem = await _stockItems.GetByWarehouseAndProductAsync(
            warehouse.Id, request.ProductId, cancellationToken);
        if (stockItem is null)
        {
            return Result.Failure<StockItemDto>(StockItemErrors.NotFound);
        }

        return StockItemDto.FromDomain(stockItem);
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.SetReorderThreshold;

/// <summary>
/// Yeniden-sipariş eşiğini ayarlayan handler (docs/06 S2.1 stok uyarıları). Ürünün VARSAYILAN
/// depodaki stok kalemini bulur, domain <c>SetReorderThreshold</c> ile eşiği ayarlar (negatif kontrol
/// domain'de) ve SaveChanges ile atomik kaydeder. Bu işlem hareket üretmez; uyarı, eşiği aşan bir
/// çıkış hareketiyle (satış/fire) yayınlanır. Finance.RecordCollectionHandler deseniyle birebir.
/// </summary>
internal sealed class SetReorderThresholdHandler : ICommandHandler<SetReorderThresholdCommand, Guid>
{
    private readonly IStockItemRepository _stockItems;
    private readonly IWarehouseRepository _warehouses;
    private readonly IUnitOfWork _unitOfWork;

    public SetReorderThresholdHandler(
        IStockItemRepository stockItems,
        IWarehouseRepository warehouses,
        IUnitOfWork unitOfWork)
    {
        _stockItems = stockItems;
        _warehouses = warehouses;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(SetReorderThresholdCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouses.GetDefaultAsync(cancellationToken);
        if (warehouse is null)
        {
            return Result.Failure<Guid>(StockItemErrors.NotFound);
        }

        var stockItem = await _stockItems.GetByWarehouseAndProductAsync(
            warehouse.Id, request.ProductId, cancellationToken);
        if (stockItem is null)
        {
            return Result.Failure<Guid>(StockItemErrors.NotFound);
        }

        var result = stockItem.SetReorderThreshold(request.ReorderThreshold);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _stockItems.Update(stockItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stockItem.Id;
    }
}

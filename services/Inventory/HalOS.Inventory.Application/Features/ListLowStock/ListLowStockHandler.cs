using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.ListLowStock;

/// <summary>
/// Düşük stok listesi query handler (docs/06 S2.1 stok uyarıları). Eşik tanımlı ve kalanı eşiğe/altına
/// inmiş kalemleri repository'den getirir (tenant filtreli, BK-8). SALT-OKUMA CQRS.
/// </summary>
internal sealed class ListLowStockHandler : IQueryHandler<ListLowStockQuery, IReadOnlyList<StockItemDto>>
{
    private readonly IStockItemRepository _stockItems;

    public ListLowStockHandler(IStockItemRepository stockItems)
    {
        _stockItems = stockItems;
    }

    public async Task<Result<IReadOnlyList<StockItemDto>>> Handle(
        ListLowStockQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _stockItems.ListLowStockAsync(cancellationToken);
        IReadOnlyList<StockItemDto> dto = items.Select(StockItemDto.FromDomain).ToList();
        return Result.Success(dto);
    }
}

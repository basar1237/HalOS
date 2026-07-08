using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.GetStock;

/// <summary>
/// Bir ürünün stok kalemini kalan miktarıyla getirir (docs/02 §115; tenant + ürün başına tek kalem).
/// </summary>
public sealed record GetStockQuery(Guid ProductId) : IQuery<StockItemDto>;

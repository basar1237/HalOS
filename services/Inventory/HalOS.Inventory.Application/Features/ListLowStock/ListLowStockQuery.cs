using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.ListLowStock;

/// <summary>
/// Düşük stok listesi (docs/06 S2.1 stok uyarıları): yeniden-sipariş eşiği tanımlı ve eldeki miktarı
/// eşiğe eşit veya altında olan stok kalemleri. Tenant global filter otomatik uygulanır (BK-8).
/// </summary>
public sealed record ListLowStockQuery : IQuery<IReadOnlyList<StockItemDto>>;

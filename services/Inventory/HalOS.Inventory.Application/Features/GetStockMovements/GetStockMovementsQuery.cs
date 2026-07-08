using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.GetStockMovements;

/// <summary>
/// Bir ürünün stok hareket dökümünü (giriş/çıkış/fire + kalan) getirir (docs/02 §115). Cari ekstre
/// (GetStatement) deseniyle birebir.
/// </summary>
public sealed record GetStockMovementsQuery(Guid ProductId) : IQuery<StockMovementsDto>;

using HalOS.BuildingBlocks.Application;

namespace HalOS.Inventory.Application.Features.SetReorderThreshold;

/// <summary>
/// Bir ürünün VARSAYILAN depodaki stok kalemi için yeniden-sipariş eşiğini ayarlar/kaldırır
/// (docs/06 S2.1 stok uyarıları). Eşik null ise uyarı devre dışı kalır. Yetki: Depo/Yönetici
/// (docs/03 §3). Kalan eşiğe/altına indiğinde LowStockAlerted yayınlanır (çıkış hareketiyle).
/// </summary>
/// <param name="ProductId">Eşiği ayarlanacak ürün (Product ID).</param>
/// <param name="ReorderThreshold">Yeniden-sipariş eşiği (NUMERIC(18,3); decimal — BK-2). Null: kaldır.</param>
public sealed record SetReorderThresholdCommand(
    Guid ProductId,
    decimal? ReorderThreshold) : ICommand<Guid>;

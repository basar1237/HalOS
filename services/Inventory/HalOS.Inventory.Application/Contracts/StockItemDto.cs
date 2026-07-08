using HalOS.Inventory.Domain.Aggregates;
using HalOS.Inventory.Domain.Enums;

namespace HalOS.Inventory.Application.Contracts;

/// <summary>Stok hareketi okuma DTO'su (docs/02 §115). Domain entity'si API'ye sızmaz.</summary>
public sealed record StockMovementDto(
    Guid Id,
    StockMovementKind Kind,
    decimal SignedQuantity,
    Guid? RefId,
    string? Reason,
    DateTime OccurredAt)
{
    public static StockMovementDto FromDomain(StockMovement movement) => new(
        movement.Id,
        movement.Kind,
        movement.SignedQuantity,
        movement.RefId,
        movement.Reason,
        movement.OccurredAt);
}

/// <summary>
/// Stok kalemi okuma DTO'su (docs/02 §115 <c>StockItem</c>; docs/06 S2.1 depo lokasyonu + stok
/// uyarıları). Kalan (QuantityOnHand) türetilmiş değerdir (Σ hareket). Depo (WarehouseId) ve
/// yeniden-sipariş eşiği (ReorderThreshold) dahildir. Domain aggregate'i API'ye sızmaz.
/// </summary>
public sealed record StockItemDto(
    Guid Id,
    Guid TenantId,
    Guid WarehouseId,
    Guid ProductId,
    decimal QuantityOnHand,
    decimal? ReorderThreshold,
    int MovementCount)
{
    public static StockItemDto FromDomain(StockItem item) => new(
        item.Id,
        item.TenantId,
        item.WarehouseId,
        item.ProductId,
        item.QuantityOnHand,
        item.ReorderThreshold,
        item.Movements.Count);
}

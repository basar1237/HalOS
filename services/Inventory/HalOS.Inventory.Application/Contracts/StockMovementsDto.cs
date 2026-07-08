using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Contracts;

/// <summary>
/// Stok kaleminin hareket dökümü DTO'su (docs/02 §115): kalem kimliği, ürün, güncel kalan ve
/// hareketler (oluşma zamanına göre artan). Cari ekstre (StatementDto) deseniyle birebir.
/// </summary>
public sealed record StockMovementsDto(
    Guid StockItemId,
    Guid ProductId,
    decimal QuantityOnHand,
    IReadOnlyList<StockMovementDto> Movements)
{
    public static StockMovementsDto FromDomain(StockItem item) => new(
        item.Id,
        item.ProductId,
        item.QuantityOnHand,
        item.Movements
            .OrderBy(m => m.OccurredAt)
            .Select(StockMovementDto.FromDomain)
            .ToList());
}

using HalOS.Inventory.Domain.Aggregates;
using HalOS.Inventory.Domain.Enums;

namespace HalOS.Inventory.Application.Contracts;

/// <summary>Ürün okuma DTO'su (docs/05 §3.3). Domain aggregate'i API'ye sızmaz.</summary>
/// <param name="Id">Ürün kimliği (satış/mal-geliş satırında productId olarak referanslanır).</param>
/// <param name="Name">Ürünün görünen adı.</param>
/// <param name="Category">Ürün kategorisi (opsiyonel).</param>
/// <param name="DefaultUnit">Varsayılan ölçü birimi (JSON'da int — bkz. UnitOfMeasure).</param>
/// <param name="IsActive">Aktif mi (pasif ürün seçicide gösterilmez).</param>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Category,
    UnitOfMeasure DefaultUnit,
    bool IsActive)
{
    public static ProductDto FromDomain(Product product) => new(
        product.Id,
        product.Name,
        product.Category,
        product.DefaultUnit,
        product.IsActive);
}

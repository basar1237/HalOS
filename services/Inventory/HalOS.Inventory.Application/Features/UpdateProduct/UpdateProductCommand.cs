using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Domain.Enums;

namespace HalOS.Inventory.Application.Features.UpdateProduct;

/// <summary>Ürün günceller (ad/kategori/varsayılan birim; docs/03 M2). Yetki: Patron/Yönetici.</summary>
public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Category,
    UnitOfMeasure DefaultUnit) : ICommand<Guid>;

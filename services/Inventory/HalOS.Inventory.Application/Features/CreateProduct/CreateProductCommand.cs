using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Domain.Enums;

namespace HalOS.Inventory.Application.Features.CreateProduct;

/// <summary>
/// Yeni ürün oluşturur (docs/03 M2; docs/05 §3.3). Ad zorunlu; kategori opsiyonel; varsayılan birim
/// (satış/mal-geliş satırında ön-seçili). Yetki: Patron/Yönetici (docs/03 §3 "Ürün & Birim | Yönetici").
/// </summary>
public sealed record CreateProductCommand(
    string Name,
    string? Category,
    UnitOfMeasure DefaultUnit) : ICommand<Guid>;

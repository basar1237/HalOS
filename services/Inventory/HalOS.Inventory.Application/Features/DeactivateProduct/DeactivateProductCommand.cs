using HalOS.BuildingBlocks.Application;

namespace HalOS.Inventory.Application.Features.DeactivateProduct;

/// <summary>Ürünü pasifleştirir (soft-delete; docs/03 M2). Yetki: Patron/Yönetici.</summary>
public sealed record DeactivateProductCommand(Guid Id) : ICommand<Guid>;

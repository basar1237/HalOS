using HalOS.BuildingBlocks.Application;

namespace HalOS.Inventory.Application.Features.CreateWarehouse;

/// <summary>
/// Yeni bir depo oluşturur (docs/06 S2.1 depo lokasyonu). Kod tenant içinde tekildir. Yetki:
/// Depo/Yönetici/Patron (docs/03 §3). Bir tenant'ın birden çok deposu olabilir; varsayılan depo
/// olay-güdümlü giriş/çıkış için kullanılır.
/// </summary>
/// <param name="Name">Deponun görünen adı.</param>
/// <param name="Code">Tenant içinde tekil kısa kod.</param>
/// <param name="IsDefault">Varsayılan depo olarak işaretlenip işaretlenmeyeceği.</param>
public sealed record CreateWarehouseCommand(
    string Name,
    string Code,
    bool IsDefault) : ICommand<Guid>;

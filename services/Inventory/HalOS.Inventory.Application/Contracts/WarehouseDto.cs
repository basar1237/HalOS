using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Contracts;

/// <summary>
/// Depo okuma DTO'su (docs/06 S2.1 depo lokasyonu). Domain aggregate'i API'ye sızmaz.
/// </summary>
/// <param name="Id">Depo kimliği.</param>
/// <param name="Name">Deponun görünen adı.</param>
/// <param name="Code">Deponun tenant içinde tekil kısa kodu.</param>
/// <param name="IsDefault">Tenant'ın varsayılan deposu mu.</param>
public sealed record WarehouseDto(
    Guid Id,
    string Name,
    string Code,
    bool IsDefault)
{
    public static WarehouseDto FromDomain(Warehouse warehouse) => new(
        warehouse.Id,
        warehouse.Name,
        warehouse.Code,
        warehouse.IsDefault);
}

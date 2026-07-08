using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.ListWarehouses;

/// <summary>Tenant'ın depolarını listeleyen query handler (docs/06 S2.1). Tenant filtreli (BK-8).</summary>
internal sealed class ListWarehousesHandler : IQueryHandler<ListWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    private readonly IWarehouseRepository _warehouses;

    public ListWarehousesHandler(IWarehouseRepository warehouses)
    {
        _warehouses = warehouses;
    }

    public async Task<Result<IReadOnlyList<WarehouseDto>>> Handle(
        ListWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        var warehouses = await _warehouses.ListAsync(cancellationToken);
        IReadOnlyList<WarehouseDto> dto = warehouses.Select(WarehouseDto.FromDomain).ToList();
        return Result.Success(dto);
    }
}

using HalOS.Inventory.Api.Authorization;
using HalOS.Inventory.Api.Contracts;
using HalOS.Inventory.Application.Features.CreateWarehouse;
using HalOS.Inventory.Application.Features.ListWarehouses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Inventory.Api.Controllers;

/// <summary>
/// Depo (Warehouse) uçları (docs/06 S2.1 depo lokasyonu). Tenant JWT claim'inden çözülür ve global
/// query filter'a taşınır (BK-8). RBAC (docs/03 §3): depo oluştur/listele → Patron/Yönetici/Depo.
/// </summary>
[ApiController]
[Route("warehouses")]
[Authorize]
public sealed class WarehousesController : ControllerBase
{
    private readonly ISender _sender;

    public WarehousesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Tenant'ın depolarını listeler. Okuma yetkisi.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StockRead)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListWarehousesQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Yeni bir depo oluşturur. Yetki: Patron/Yönetici/Depo.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.WarehouseWrite)]
    public async Task<IActionResult> Create(CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateWarehouseCommand(request.Name, request.Code, request.IsDefault);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}

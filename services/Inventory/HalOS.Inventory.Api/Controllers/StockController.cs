using HalOS.Inventory.Api.Authorization;
using HalOS.Inventory.Application.Features.GetStock;
using HalOS.Inventory.Application.Features.GetStockMovements;
using HalOS.Inventory.Application.Features.ListStock;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Inventory.Api.Controllers;

/// <summary>
/// Stok okuma uçları (docs/03 M9; docs/02 §115 Stok &amp; Depo). Tenant JWT claim'inden çözülür ve
/// global query filter'a taşınır (BK-8). RBAC (docs/03 §3): okuma Patron/Yönetici/Depo.
/// </summary>
[ApiController]
[Route("stock")]
[Authorize]
public sealed class StockController : ControllerBase
{
    private readonly ISender _sender;

    public StockController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Sayfalanmış stok kalemi listesi (kalan özetli). Okuma yetkisi.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StockRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListStockQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Bir ürünün stok kalemi (kalan miktar). Okuma yetkisi.</summary>
    [HttpGet("{productId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StockRead)]
    public async Task<IActionResult> GetByProduct(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStockQuery(productId), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Bir ürünün stok hareket dökümü (giriş/çıkış/fire + kalan). Okuma yetkisi.</summary>
    [HttpGet("{productId:guid}/movements")]
    [Authorize(Policy = AuthorizationPolicies.StockRead)]
    public async Task<IActionResult> GetMovements(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStockMovementsQuery(productId), cancellationToken);
        return result.ToActionResult(this);
    }
}

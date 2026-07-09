using HalOS.Inventory.Api.Authorization;
using HalOS.Inventory.Api.Contracts;
using HalOS.Inventory.Application.Features.CreateProduct;
using HalOS.Inventory.Application.Features.DeactivateProduct;
using HalOS.Inventory.Application.Features.GetProduct;
using HalOS.Inventory.Application.Features.ListProducts;
using HalOS.Inventory.Application.Features.UpdateProduct;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Inventory.Api.Controllers;

/// <summary>
/// Ürün kataloğu uçları (docs/03 M2; docs/05 §3.3). Tenant JWT claim'inden çözülür ve global query
/// filter'a taşınır (BK-8). RBAC (docs/03 §3): okuma Patron/Yönetici/Depo, yazma Patron/Yönetici.
/// Satış/mal-geliş satırları ürünü buradaki Id ile referanslar.
/// </summary>
[ApiController]
[Route("products")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Sayfalanmış ürün listesi (ada göre). onlyActive=true (varsayılan) yalnız aktifler.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ProductRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListProductsQuery(page, pageSize, onlyActive),
            cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Tekil ürün. Okuma yetkisi.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProductRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetProductQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Yeni ürün oluşturur. Yazma yetkisi: Patron/Yönetici.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ProductWrite)]
    public async Task<IActionResult> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(request.Name, request.Category, request.DefaultUnit);
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>Ürün günceller. Yazma yetkisi: Patron/Yönetici.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProductWrite)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(id, request.Name, request.Category, request.DefaultUnit);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Ürünü pasifleştirir (soft-delete). Yazma yetkisi: Patron/Yönetici.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProductWrite)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateProductCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}

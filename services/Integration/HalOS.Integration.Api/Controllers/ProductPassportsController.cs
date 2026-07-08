using HalOS.Integration.Api.Authorization;
using HalOS.Integration.Application.Features.GetProductPassport;
using HalOS.Integration.Application.Features.ListProductPassports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Integration.Api.Controllers;

/// <summary>
/// Künye (ProductPassport) uçları (docs/03 M8; §5 "e-Belge Merkezi"). Künyeler ConsignmentReceived
/// event'i tüketilerek OTOMATİK üretilir (mal geliş kalemi başına, HKS 19-haneli kod); bu uçlar okuma
/// sağlar. Tenant JWT claim'inden çözülür ve global query filter'a taşınır (BK-8). RBAC (docs/03 §3/§5):
/// Patron/Yönetici/Muhasebe + Depo.
/// </summary>
[ApiController]
[Route("product-passports")]
[Authorize]
public sealed class ProductPassportsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductPassportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Sayfalanmış künye listesi. Okuma yetkisi.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ProductPassportRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListProductPassportsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Bir künyeyi kimliğiyle getirir (HKS 19-haneli kod dahil). Okuma yetkisi.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProductPassportRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetProductPassportQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }
}

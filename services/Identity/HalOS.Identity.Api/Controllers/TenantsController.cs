using HalOS.Identity.Api.Authorization;
using HalOS.Identity.Api.Contracts;
using HalOS.Identity.Application.Features.Tenants.CreateTenant;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Identity.Api.Controllers;

/// <summary>
/// Tenant CRUD iskeleti (docs/06 S0.4). Bu fazda oluşturma ucu tamamlanmıştır; listeleme/
/// güncelleme/silme uçları iskelet olarak bırakılmıştır (bkz. notes).
/// </summary>
[ApiController]
[Route("tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly ISender _sender;

    public TenantsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Yeni işletme oluşturur. İlk kurulum (self-signup) senaryosunda anonim; ileride
    /// platform yönetici politikasına bağlanacak (bkz. notes).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(
        CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateTenantCommand(request.Name), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>Tekil tenant (iskelet — okuma modeli sonraki fazda).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
    public IActionResult GetById(Guid id)
    {
        // İskelet: okuma modeli/handler sonraki fazda eklenecek (docs/06 S0.4).
        return Ok(new { id });
    }
}

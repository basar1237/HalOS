using HalOS.Party.Api.Authorization;
using HalOS.Party.Api.Contracts;
using HalOS.Party.Application.Features.AddPartyRole;
using HalOS.Party.Application.Features.CreateParty;
using HalOS.Party.Application.Features.DeactivateParty;
using HalOS.Party.Application.Features.GetParty;
using HalOS.Party.Application.Features.ListParties;
using HalOS.Party.Application.Features.UpdateParty;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Party.Api.Controllers;

/// <summary>
/// Taraflar (Cari kartlar) CRUD + rol uçları (docs/03 M1). Tenant JWT claim'inden çözülür
/// ve global query filter'a taşınır (BK-8). RBAC: yazma Patron/Yönetici, okuma Muhasebe/Yönetici
/// (docs/03 §3).
/// </summary>
[ApiController]
[Route("parties")]
[Authorize]
public sealed class PartiesController : ControllerBase
{
    private readonly ISender _sender;

    public PartiesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Yeni taraf oluşturur. Yazma yetkisi: Patron/Yönetici.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PartyWrite)]
    public async Task<IActionResult> Create(
        CreatePartyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePartyCommand(
            request.DisplayName,
            request.Tckn,
            request.Vkn,
            request.TaxOffice,
            request.Phone,
            request.Address,
            request.KeepsRecords,
            request.WithholdingProfile,
            request.Roles);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>Tekil taraf. Okuma yetkisi: Muhasebe/Yönetici/Patron.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PartyRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPartyQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Sayfalanmış taraf listesi. Okuma yetkisi: Muhasebe/Yönetici/Patron.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.PartyRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListPartiesQuery(page, pageSize, onlyActive),
            cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Taraf günceller. Yazma yetkisi: Patron/Yönetici.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PartyWrite)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdatePartyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePartyCommand(
            id,
            request.DisplayName,
            request.TaxOffice,
            request.Phone,
            request.Address,
            request.KeepsRecords,
            request.WithholdingProfile);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Tarafı pasifleştirir (soft-delete). Yazma yetkisi: Patron/Yönetici.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PartyWrite)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivatePartyCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Tarafa rol ekler. Yazma yetkisi: Patron/Yönetici.</summary>
    [HttpPost("{id:guid}/roles")]
    [Authorize(Policy = AuthorizationPolicies.PartyWrite)]
    public async Task<IActionResult> AddRole(
        Guid id,
        AddPartyRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AddPartyRoleCommand(id, request.Type), cancellationToken);
        return result.ToActionResult(this);
    }
}

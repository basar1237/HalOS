using HalOS.Sales.Api.Authorization;
using HalOS.Sales.Api.Contracts;
using HalOS.Sales.Application.Features.ReceiveConsignment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Sales.Api.Controllers;

/// <summary>
/// Mal geliş (Consignment) uçları (docs/03 M3). Tenant JWT claim'inden çözülür ve global query
/// filter'a taşınır (BK-8). RBAC: mal geliş kabul Patron/Yönetici/Kasiyer/Depo (docs/03 §3).
/// </summary>
[ApiController]
[Route("consignments")]
[Authorize]
public sealed class ConsignmentsController : ControllerBase
{
    private readonly ISender _sender;

    public ConsignmentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Müstahsilden mal geliş kabul eder. Yetki: Patron/Yönetici/Kasiyer/Depo.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ConsignmentWrite)]
    public async Task<IActionResult> Receive(
        ReceiveConsignmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReceiveConsignmentCommand(
            request.ProducerPartyId,
            request.ReceivedAt,
            request.DispatchNoteRef,
            request.Items);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return CreatedAtAction(null, new { id = result.Value }, new { id = result.Value });
    }
}

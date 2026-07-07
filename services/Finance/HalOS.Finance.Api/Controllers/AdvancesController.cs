using HalOS.Finance.Api.Authorization;
using HalOS.Finance.Api.Contracts;
using HalOS.Finance.Application.Features.RecordAdvance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Finance.Api.Controllers;

/// <summary>
/// Avans uçları (docs/03 M6; docs/02 §3.4 avans mahsuplaşır). Tenant JWT claim'inden çözülür
/// (BK-8). RBAC (docs/03 §3): mali işlem → Patron/Yönetici/Muhasebe. BK-6: 7.000 TL üstü nakit
/// reddedilir.
/// </summary>
[ApiController]
[Route("advances")]
[Authorize]
public sealed class AdvancesController : ControllerBase
{
    private readonly ISender _sender;

    public AdvancesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Avans kaydeder. Yetki: Patron/Yönetici/Muhasebe.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdvanceWrite)]
    public async Task<IActionResult> Record(RecordAdvanceRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordAdvanceCommand(
            request.PartyId,
            request.Amount,
            request.Channel,
            request.BankReference,
            request.OccurredAt);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}

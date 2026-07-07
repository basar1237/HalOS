using HalOS.Finance.Api.Authorization;
using HalOS.Finance.Api.Contracts;
using HalOS.Finance.Application.Features.RecordCollection;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Finance.Api.Controllers;

/// <summary>
/// Alıcıdan tahsilat uçları (docs/03 M6). Tenant JWT claim'inden çözülür (BK-8). RBAC (docs/03 §3):
/// tahsilat gir (alıcıdan) → Patron/Yönetici/Muhasebe/Kasiyer. BK-6: 7.000 TL üstü nakit reddedilir.
/// </summary>
[ApiController]
[Route("collections")]
[Authorize]
public sealed class CollectionsController : ControllerBase
{
    private readonly ISender _sender;

    public CollectionsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Alıcıdan tahsilat kaydeder. Yetki: Patron/Yönetici/Muhasebe/Kasiyer.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CollectionWrite)]
    public async Task<IActionResult> Record(RecordCollectionRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordCollectionCommand(
            request.PartyId,
            request.Amount,
            request.Channel,
            request.BankReference,
            request.OccurredAt);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}

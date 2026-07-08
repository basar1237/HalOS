using HalOS.Inventory.Api.Authorization;
using HalOS.Inventory.Api.Contracts;
using HalOS.Inventory.Application.Features.RecordSpoilage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Inventory.Api.Controllers;

/// <summary>
/// Fire (zayiat) kaydı uçları (docs/03 M9 / BK-7; docs/02 §57 Fire=Spoilage, §237 SpoilageRecorded).
/// Tenant JWT claim'inden çözülür (BK-8). RBAC (docs/03 §3): fire kaydet → Patron/Yönetici/Depo.
/// BK-7: fire mevcut stoğu aşamaz (kalan negatif olamaz).
/// </summary>
[ApiController]
[Route("spoilage")]
[Authorize]
public sealed class SpoilageController : ControllerBase
{
    private readonly ISender _sender;

    public SpoilageController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Bir ürün için fire kaydeder. Yetki: Patron/Yönetici/Depo.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SpoilageWrite)]
    public async Task<IActionResult> Record(RecordSpoilageRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordSpoilageCommand(
            request.ProductId,
            request.Quantity,
            request.Reason,
            request.OccurredAt);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}

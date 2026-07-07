using HalOS.Finance.Api.Authorization;
using HalOS.Finance.Api.Contracts;
using HalOS.Finance.Application.Features.RecordPayment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Finance.Api.Controllers;

/// <summary>
/// Müstahsile ödeme uçları (docs/03 M6). Tenant JWT claim'inden çözülür (BK-8). RBAC (docs/03 §3):
/// ödeme yap (müstahsile) → Patron/Yönetici/Muhasebe. BK-6: 7.000 TL üstü nakit reddedilir.
/// </summary>
[ApiController]
[Route("payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Müstahsile ödeme kaydeder. Yetki: Patron/Yönetici/Muhasebe.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PaymentWrite)]
    public async Task<IActionResult> Record(RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordPaymentCommand(
            request.PartyId,
            request.Amount,
            request.Channel,
            request.BankReference,
            request.OccurredAt);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}

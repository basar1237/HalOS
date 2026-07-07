using HalOS.Integration.Api.Authorization;
using HalOS.Integration.Application.Features.GetHksNotification;
using HalOS.Integration.Application.Features.ListHksNotifications;
using HalOS.Integration.Application.Features.ReissueHksNotification;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Integration.Api.Controllers;

/// <summary>
/// HKS bildirimi uçları (docs/03 M8; §5 "e-Belge Merkezi"). Bildirimler SaleCompleted event'i
/// tüketilerek HER satış için OTOMATİK üretilir (BK-4); bu uçlar okuma + red yönetimi (yeniden gönderim)
/// sağlar. Tenant JWT claim'inden çözülür ve global query filter'a taşınır (BK-8). RBAC (docs/03 §3):
/// Patron/Yönetici/Muhasebe. e-MM ProducerReceiptsController deseniyle birebir.
/// </summary>
[ApiController]
[Route("hks-notifications")]
[Authorize]
public sealed class HksNotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public HksNotificationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Sayfalanmış HKS bildirimi listesi. Okuma yetkisi.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.HksNotificationRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListHksNotificationsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Bir HKS bildirimini kimliğiyle getirir. Okuma yetkisi.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.HksNotificationRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetHksNotificationQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Başarısız/taslak bir HKS bildirimini yeniden gönderir (red yönetimi). Yeniden gönderim yetkisi.</summary>
    [HttpPost("{id:guid}/reissue")]
    [Authorize(Policy = AuthorizationPolicies.HksNotificationReissue)]
    public async Task<IActionResult> Reissue(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReissueHksNotificationCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}

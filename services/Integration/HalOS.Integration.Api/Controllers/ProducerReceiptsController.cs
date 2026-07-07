using HalOS.Integration.Api.Authorization;
using HalOS.Integration.Application.Features.GetProducerReceipt;
using HalOS.Integration.Application.Features.ListProducerReceipts;
using HalOS.Integration.Application.Features.ReissueProducerReceipt;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Integration.Api.Controllers;

/// <summary>
/// e-Müstahsil Makbuzu (e-MM) uçları (docs/03 M7; §5 "e-Belge Merkezi"). Belgeler SaleCompleted
/// event'i tüketilerek OTOMATİK üretilir (kayıt tutmayan müstahsil için, BK-4); bu uçlar okuma +
/// red yönetimi (yeniden gönderim) sağlar. Tenant JWT claim'inden çözülür ve global query filter'a
/// taşınır (BK-8). RBAC (docs/03 §3): Patron/Yönetici/Muhasebe.
/// </summary>
[ApiController]
[Route("producer-receipts")]
[Authorize]
public sealed class ProducerReceiptsController : ControllerBase
{
    private readonly ISender _sender;

    public ProducerReceiptsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Sayfalanmış e-MM listesi. Okuma yetkisi.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ProducerReceiptRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListProducerReceiptsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Bir e-MM belgesini kimliğiyle getirir. Okuma yetkisi.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProducerReceiptRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetProducerReceiptQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Başarısız/taslak bir e-MM'i GİB'e yeniden gönderir (red yönetimi). Yeniden gönderim yetkisi.</summary>
    [HttpPost("{id:guid}/reissue")]
    [Authorize(Policy = AuthorizationPolicies.ProducerReceiptReissue)]
    public async Task<IActionResult> Reissue(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReissueProducerReceiptCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}

using HalOS.Integration.Api.Authorization;
using HalOS.Integration.Application.Features.GetInvoice;
using HalOS.Integration.Application.Features.ListInvoices;
using HalOS.Integration.Application.Features.ReissueInvoice;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Integration.Api.Controllers;

/// <summary>
/// e-Fatura (HAL) uçları (docs/03 M8; §5 "e-Belge Merkezi"). Faturalar SaleCompleted event'i tüketilerek
/// HER satış için OTOMATİK üretilir (BK-4); bu uçlar okuma + red yönetimi (yeniden gönderim) sağlar.
/// Tenant JWT claim'inden çözülür ve global query filter'a taşınır (BK-8). RBAC (docs/03 §3):
/// Patron/Yönetici/Muhasebe. e-MM ProducerReceiptsController deseniyle birebir.
/// </summary>
[ApiController]
[Route("invoices")]
[Authorize]
public sealed class InvoicesController : ControllerBase
{
    private readonly ISender _sender;

    public InvoicesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Sayfalanmış e-Fatura listesi. Okuma yetkisi.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.InvoiceRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListInvoicesQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Bir e-Fatura belgesini kimliğiyle getirir. Okuma yetkisi.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.InvoiceRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetInvoiceQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Başarısız/taslak bir e-Fatura'yı GİB'e yeniden gönderir (red yönetimi). Yeniden gönderim yetkisi.</summary>
    [HttpPost("{id:guid}/reissue")]
    [Authorize(Policy = AuthorizationPolicies.InvoiceReissue)]
    public async Task<IActionResult> Reissue(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReissueInvoiceCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}

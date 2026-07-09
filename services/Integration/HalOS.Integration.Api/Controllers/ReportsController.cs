using HalOS.Integration.Api.Authorization;
using HalOS.Integration.Application.Features.PendingDocuments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Integration.Api.Controllers;

/// <summary>
/// e-Belge tarafı özet raporları (dashboard) — SALT-OKUMA CQRS. Tenant JWT claim'inden çözülür ve
/// global query filter'a taşınır (BK-8). RBAC: Patron/Yönetici/Muhasebe (InvoiceRead ile aynı sınıf).
/// </summary>
[ApiController]
[Route("reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Bekleyen (Draft/Failed) e-belge sayıları: e-Fatura + e-MM + HKS bildirimi (dashboard
    /// "Bekleyen e-Belge" kartı).
    /// </summary>
    [HttpGet("pending-documents")]
    [Authorize(Policy = AuthorizationPolicies.InvoiceRead)]
    public async Task<IActionResult> PendingDocuments(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PendingDocumentsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }
}

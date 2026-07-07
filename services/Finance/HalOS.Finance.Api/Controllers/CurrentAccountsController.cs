using HalOS.Finance.Api.Authorization;
using HalOS.Finance.Application.Features.GetCurrentAccount;
using HalOS.Finance.Application.Features.GetStatement;
using HalOS.Finance.Application.Features.ListCurrentAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Finance.Api.Controllers;

/// <summary>
/// Cari hesap okuma uçları (docs/03 M6; §5 "Cari Kartları"/"Cari Detay/Ekstre"). Tenant JWT
/// claim'inden çözülür ve global query filter'a taşınır (BK-8). RBAC (docs/03 §3): okuma
/// Patron/Yönetici/Muhasebe.
/// </summary>
[ApiController]
[Route("current-accounts")]
[Authorize]
public sealed class CurrentAccountsController : ControllerBase
{
    private readonly ISender _sender;

    public CurrentAccountsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Sayfalanmış cari hesap listesi (bakiye özetli). Okuma yetkisi.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CurrentAccountRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListCurrentAccountsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Bir tarafın (Party) cari hesabı (bakiye). Okuma yetkisi.</summary>
    [HttpGet("{partyId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CurrentAccountRead)]
    public async Task<IActionResult> GetByParty(Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentAccountQuery(partyId), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Bir tarafın cari ekstresi (hareketler + bakiye). Okuma yetkisi.</summary>
    [HttpGet("{partyId:guid}/statement")]
    [Authorize(Policy = AuthorizationPolicies.CurrentAccountRead)]
    public async Task<IActionResult> GetStatement(Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStatementQuery(partyId), cancellationToken);
        return result.ToActionResult(this);
    }
}

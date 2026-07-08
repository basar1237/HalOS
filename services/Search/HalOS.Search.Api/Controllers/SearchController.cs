using HalOS.BuildingBlocks.Application;
using HalOS.Search.Api.Authorization;
using HalOS.Search.Application.Search;
using HalOS.Search.Domain.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalOS.Search.Api.Controllers;

/// <summary>
/// Arama ucu (docs/06 S2.3 — "Ali'nin her şeyini 1 sn'de"). Tenant JWT tenant claim'inden çözülür
/// ve arama SADECE o tenant'a kısıtlanır (BK-8, çapraz-tenant sızıntısı YASAK) — tenant istemci
/// girdisinden ALINMAZ. RBAC (docs/03 §3): okuma → Patron/Yönetici/Muhasebe/Kasiyer.
/// </summary>
[ApiController]
[Route("search")]
[Authorize]
public sealed class SearchController : ControllerBase
{
    private const int DefaultSize = 20;
    private const int MaxSize = 100;

    private readonly SearchQueryHandler _handler;
    private readonly ITenantContext _tenantContext;

    public SearchController(SearchQueryHandler handler, ITenantContext tenantContext)
    {
        _handler = handler;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Serbest-metin arama. <paramref name="q"/> sorgu, <paramref name="type"/> opsiyonel tür filtresi
    /// (Party/Sale — case-insensitive kabul edilir, kanonik değere çevrilir), <paramref name="size"/>
    /// azami sonuç. Yetki: Patron/Yönetici/Muhasebe/Kasiyer. Bilinmeyen <paramref name="type"/> → 400.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SearchRead)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] string? type,
        [FromQuery] int? size,
        CancellationToken cancellationToken)
    {
        // Tenant JWT'den; yoksa arama yapılmaz (BK-8). Authorize sayesinde normalde her zaman dolu.
        if (!_tenantContext.HasTenant)
        {
            return Forbid();
        }

        // Tür filtresini TEK noktada kanonikleştir: böylece InMemory ve ES aynı değeri alır ve iki
        // backend AYNI sonucu verir (ES keyword term filter case-sensitive'dir). Bilinmeyen tür → 400.
        if (!SearchDocumentType.TryNormalize(type, out var normalizedType))
        {
            return BadRequest($"Geçersiz tür filtresi: '{type}'. Beklenen: {SearchDocumentType.Party} veya {SearchDocumentType.Sale}.");
        }

        var effectiveSize = Math.Clamp(size ?? DefaultSize, 1, MaxSize);
        var query = new SearchQuery(q ?? string.Empty, normalizedType, effectiveSize);

        var result = await _handler.HandleAsync(_tenantContext.TenantId, query, cancellationToken);
        return Ok(result);
    }
}

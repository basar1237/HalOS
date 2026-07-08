using HalOS.Search.Application.Abstractions;

namespace HalOS.Search.Application.Search;

/// <summary>
/// Arama sorgusu işleyicisi (docs/06 S2.3). <see cref="ISearchIndex"/> üzerinde tenant-kapsamlı arama
/// yapar. Tenant AYRI parametredir (istemci girdisi değil, JWT'den) — çapraz-tenant sızıntısı YASAK
/// (BK-8). Boş sorguda arama deposuna gitmeden boş sonuç döner. Hafif servis olduğundan MediatR
/// pipeline'ı yerine doğrudan çağrılan sade bir handler kullanılır.
/// </summary>
public sealed class SearchQueryHandler
{
    private readonly ISearchIndex _index;

    public SearchQueryHandler(ISearchIndex index)
    {
        _index = index;
    }

    /// <summary>
    /// Verilen tenant kapsamında aramayı çalıştırır. <paramref name="tenantId"/> JWT'den gelir ve
    /// aramayı SADECE o tenant'a kısıtlar (BK-8).
    /// </summary>
    public async Task<SearchResultDto> HandleAsync(
        Guid tenantId,
        SearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return new SearchResultDto(Array.Empty<SearchHit>());
        }

        var hits = await _index.SearchAsync(
            tenantId,
            query.Query,
            query.Type,
            query.Size,
            cancellationToken);

        return new SearchResultDto(hits);
    }
}

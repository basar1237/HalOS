using System.Collections.Concurrent;
using HalOS.Search.Application.Abstractions;
using HalOS.Search.Domain.Documents;

namespace HalOS.Search.Infrastructure.Search;

/// <summary>
/// <see cref="ISearchIndex"/>'in bellek-içi (in-memory) uygulaması — ES yokken/test için (KARAR:
/// STACK). Basit case-insensitive contains eşleşmesi + tenant filtresi yapar; böylece servis
/// Elasticsearch olmadan da derlenir/test edilir. <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// ile eşzamanlı consumer yazımı güvenlidir.
///
/// TENANT İZOLASYONU (BK-8): arama SADECE <paramref name="tenantId"/> eşleşen dokümanları döner;
/// çapraz-tenant sızıntısı olmaz. Bu, gerçek ES uygulamasındaki term filter'ın bellek-içi karşılığıdır.
/// </summary>
public sealed class InMemorySearchIndex : ISearchIndex
{
    private readonly ConcurrentDictionary<string, SearchDocument> _documents = new();

    /// <inheritdoc />
    public Task IndexAsync(SearchDocument document, CancellationToken cancellationToken = default)
    {
        // Idempotent upsert: aynı Id üzerine yazar (broker retry'da çift kayıt olmaz).
        _documents[document.Id] = document;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchHit>> SearchAsync(
        Guid tenantId,
        string query,
        string? type,
        int size,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || size <= 0)
        {
            return Task.FromResult<IReadOnlyList<SearchHit>>(Array.Empty<SearchHit>());
        }

        var hits = _documents.Values
            // BK-8: yalnız bu tenant'ın dokümanları — çapraz-tenant sızıntısı YASAK.
            .Where(d => d.TenantId == tenantId)
            // type KANONİK gelir (çağıran SearchDocumentType.TryNormalize ile kanonikleştirir); yine de
            // savunmacı olarak case-insensitive karşılaştırılır. ES tarafı keyword term ile birebir
            // eşler — kanonikleştirme sayesinde iki backend AYNI sonucu verir.
            .Where(d => type is null || string.Equals(d.Type, type, StringComparison.OrdinalIgnoreCase))
            .Where(d => d.SearchableText.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(size)
            .Select(d => new SearchHit(d.Id, d.Type, d.Summary))
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchHit>>(hits);
    }
}

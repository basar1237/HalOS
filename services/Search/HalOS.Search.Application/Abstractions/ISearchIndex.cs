using HalOS.Search.Domain.Documents;

namespace HalOS.Search.Application.Abstractions;

/// <summary>
/// Arama deposu soyutlaması (docs/06 S2.3; docs/04 ADR-007). İki uygulaması vardır:
/// <c>ElasticsearchSearchIndex</c> (gerçek ES) ve <c>InMemorySearchIndex</c> (ES yoksa/test —
/// basit case-insensitive contains). Uygulama katmanı yalnız bu arayüze bağlıdır; böylece servis
/// ES olmadan da derlenir/test edilir (KARAR: STACK). Indeks tenant-agnostik tek depodur; her
/// doküman <see cref="SearchDocument.TenantId"/> taşır ve arama tenant'a GÖRE filtrelenir (BK-8).
/// </summary>
public interface ISearchIndex
{
    /// <summary>
    /// Bir dokümanı indeksler (idempotent upsert — aynı <see cref="SearchDocument.Id"/> üzerine yazar).
    /// Consumer'lar event tüketiminde çağırır.
    /// </summary>
    Task IndexAsync(SearchDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen tenant kapsamında serbest-metin araması yapar (BK-8: SADECE bu tenant'ın dokümanları).
    /// </summary>
    /// <param name="tenantId">Aramanın kısıtlanacağı kiracı (JWT tenant claim'inden gelir).</param>
    /// <param name="query">Serbest-metin sorgu. Boşsa sonuç dönmez.</param>
    /// <param name="type">Opsiyonel tür filtresi (<c>SearchDocumentType</c>); null ise tüm türler.</param>
    /// <param name="size">Dönecek azami sonuç sayısı.</param>
    Task<IReadOnlyList<SearchHit>> SearchAsync(
        Guid tenantId,
        string query,
        string? type,
        int size,
        CancellationToken cancellationToken = default);
}

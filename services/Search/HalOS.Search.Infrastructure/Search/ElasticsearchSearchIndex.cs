using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using HalOS.Search.Application.Abstractions;
using HalOS.Search.Domain.Documents;
using Microsoft.Extensions.Logging;

namespace HalOS.Search.Infrastructure.Search;

/// <summary>
/// <see cref="ISearchIndex"/>'in gerçek Elasticsearch uygulaması (docs/06 S2.3; docs/04 ADR-007).
/// Tenant-agnostik TEK indekste tüm dokümanlar yaşar; her doküman <c>tenant_id</c> taşır ve arama
/// bir <see cref="TermQuery"/> ile SADECE JWT tenant'ına daraltılır — çapraz-tenant sızıntısı YASAK
/// (BK-8). Serbest-metin, dokümanın <c>searchable_text</c> alanında <see cref="MatchQuery"/> ile eşleşir.
///
/// Başlangıçta indeks/mapping <see cref="EnsureIndexAsync"/> ile idempotent oluşturulur (yoksa yaratır).
/// İndeksleme <c>Id</c> ile idempotent upsert'tir. Search salt-okuma/indeksleyici olduğundan kaynak
/// servislerin DB'sine dokunmaz, event yaymaz.
/// </summary>
public sealed class ElasticsearchSearchIndex : ISearchIndex
{
    private readonly ElasticsearchClient _client;
    private readonly string _indexName;
    private readonly ILogger<ElasticsearchSearchIndex> _logger;

    public ElasticsearchSearchIndex(
        ElasticsearchClient client,
        ElasticsearchOptions options,
        ILogger<ElasticsearchSearchIndex> logger)
    {
        _client = client;
        _indexName = options.IndexName;
        _logger = logger;
    }

    /// <summary>
    /// Arama indeksinin (mapping ile) var olduğundan emin olur; yoksa oluşturur. İdempotenttir —
    /// başlangıçta bir kez çağrılır (DI kurulumunda). ES erişilemezse hata loglanır ama fırlatılmaz
    /// (servis ES olmadan da ayağa kalkabilmeli; gerçek arama o durumda başarısız olur).
    /// </summary>
    public async Task EnsureIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _client.Indices.ExistsAsync(_indexName, cancellationToken);
            if (exists.Exists)
            {
                return;
            }

            var created = await _client.Indices.CreateAsync(_indexName, c => c
                .Mappings(m => m
                    .Properties(p => p
                        .Keyword("tenant_id")
                        .Keyword("type")
                        .Text("summary")
                        .Text("searchable_text"))),
                cancellationToken);

            if (!created.IsValidResponse)
            {
                _logger.LogWarning(
                    "Arama indeksi oluşturulamadı ({IndexName}): {Error}",
                    _indexName,
                    created.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Arama indeksi hazırlanamadı ({IndexName}).", _indexName);
        }
    }

    /// <inheritdoc />
    public async Task IndexAsync(SearchDocument document, CancellationToken cancellationToken = default)
    {
        var esDoc = new ElasticSearchDocument
        {
            Id = document.Id,
            TenantId = document.TenantId,
            Type = document.Type,
            Summary = document.Summary,
            SearchableText = document.SearchableText
        };

        var response = await _client.IndexAsync(
            esDoc,
            idx => idx.Index(_indexName).Id(document.Id),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Arama dokümanı indekslenemedi (Id={document.Id}): {response.DebugInformation}");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        Guid tenantId,
        string query,
        string? type,
        int size,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || size <= 0)
        {
            return Array.Empty<SearchHit>();
        }

        // BK-8: tenant term filter ZORUNLU — arama SADECE bu tenant'ın dokümanlarını görür.
        var filters = new List<Query>
        {
            new TermQuery { Field = "tenant_id", Value = tenantId.ToString() }
        };

        if (!string.IsNullOrWhiteSpace(type))
        {
            filters.Add(new TermQuery { Field = "type", Value = type });
        }

        var response = await _client.SearchAsync<ElasticSearchDocument>(s => s
            .Indices(_indexName)
            .Size(size)
            .Query(q => q
                .Bool(b => b
                    .Must(mu => mu.Match(ma => ma.Field("searchable_text").Query(query)))
                    .Filter(filters))),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Arama başarısız (tenant={tenantId}): {response.DebugInformation}");
        }

        return response.Documents
            .Select(d => new SearchHit(d.Id, d.Type, d.Summary))
            .ToList();
    }

    /// <summary>
    /// ES'e yazılan/okunan iç doküman şekli. Alan adları <see cref="JsonPropertyName"/> ile açıkça
    /// snake_case'e sabitlenir; böylece mapping ve term/match sorgularındaki alan adlarıyla (tenant_id,
    /// type, summary, searchable_text) BİREBİR eşleşir (serializer varsayılanına bağlı kalmaz).
    /// </summary>
    private sealed class ElasticSearchDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public Guid TenantId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("searchable_text")]
        public string SearchableText { get; set; } = string.Empty;
    }
}

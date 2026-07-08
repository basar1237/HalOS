namespace HalOS.Search.Infrastructure.Search;

/// <summary>
/// Elasticsearch bağlantı ayarları ("Elasticsearch" bölümü, docs/04 ADR-007). <see cref="Url"/>
/// tanımlı DEĞİLSE servis <c>InMemorySearchIndex</c>'e düşer (KARAR: STACK). Varsayılan yerel
/// geliştirme (docker-compose) içindir; üretimde ortam değişkeni/config ile geçersiz kılınır.
/// </summary>
public sealed class ElasticsearchOptions
{
    /// <summary>Yapılandırma bölümü adı ("Elasticsearch").</summary>
    public const string SectionName = "Elasticsearch";

    /// <summary>ES sunucu adresi (ör. http://localhost:9200). Boşsa InMemory'ye düşülür.</summary>
    public string? Url { get; set; }

    /// <summary>Tenant-agnostik tek arama indeksinin adı. Doküman <c>tenant_id</c> ile ayrışır (BK-8).</summary>
    public string IndexName { get; set; } = "halos-search";
}

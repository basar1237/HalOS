using Elastic.Clients.Elasticsearch;
using FluentAssertions;
using HalOS.Search.Domain.Documents;
using HalOS.Search.Infrastructure.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HalOS.Search.Tests;

/// <summary>
/// Gerçek Elasticsearch entegrasyon testi (docs/06 S2.3). HALOS_TEST_ELASTICSEARCH ayarlı DEĞİLSE
/// SKIP edilir (Postgres deseninin ES karşılığı) — ES çalışmıyorsa `dotnet test` yine YEŞİL. Ayarlıysa
/// gerçek ES'e yazıp okur ve tenant izolasyonunu (BK-8) doğrular. Her koşu benzersiz indeks adı
/// kullanır ve sonunda temizler.
/// </summary>
public sealed class ElasticsearchSearchIndexTests
{
    [RequiresElasticsearchFact]
    public async Task RealElasticsearch_IndexesAndSearches_WithTenantIsolation()
    {
        var url = RequiresElasticsearchFactAttribute.ResolveUrl();
        var indexName = $"halos-search-test-{Guid.NewGuid():N}";
        var options = new ElasticsearchOptions { Url = url, IndexName = indexName };

        var settings = new ElasticsearchClientSettings(new Uri(url)).DefaultIndex(indexName);
        var client = new ElasticsearchClient(settings);
        var index = new ElasticsearchSearchIndex(client, options, NullLogger<ElasticsearchSearchIndex>.Instance);

        try
        {
            await index.EnsureIndexAsync();

            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();
            var partyId = Guid.NewGuid();

            await index.IndexAsync(new PartySearchDocument
            {
                Id = PartySearchDocument.MakeId(partyId),
                TenantId = tenantA,
                Type = SearchDocumentType.Party,
                Summary = "Manav Ali (1234567890)",
                PartyId = partyId,
                DisplayName = "Manav Ali",
                TaxNumber = "1234567890",
                PartyType = "Buyer"
            });

            // ES near-real-time: aramanın belgeyi görmesi için index'i yenile.
            await client.Indices.RefreshAsync(indexName);

            var found = await index.SearchAsync(tenantA, "Ali", null, 20);
            found.Should().ContainSingle();

            // BK-8: başka tenant görmemeli.
            var leaked = await index.SearchAsync(tenantB, "Ali", null, 20);
            leaked.Should().BeEmpty();
        }
        finally
        {
            await client.Indices.DeleteAsync(indexName);
        }
    }
}

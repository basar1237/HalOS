using Elastic.Clients.Elasticsearch;
using HalOS.Search.Application.Abstractions;
using HalOS.Search.Infrastructure.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HalOS.Search.Infrastructure;

/// <summary>
/// Search.Infrastructure kompozisyon kökü (docs/07 §2; docs/04 ADR-007). <see cref="ISearchIndex"/>
/// uygulamasını yapılandırmaya göre seçer:
/// <list type="bullet">
///   <item><b>Elasticsearch:Url</b> tanımlıysa → <see cref="ElasticsearchSearchIndex"/> (gerçek ES;
///   başlangıçta indeks/mapping ensure edilir).</item>
///   <item>Tanımlı DEĞİLSE → <see cref="InMemorySearchIndex"/> (ES yoksa/test; contains eşleşme) —
///   servis ES olmadan da çalışır (KARAR: STACK). Seçim log'lanır.</item>
/// </list>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSearchInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ElasticsearchOptions.SectionName)
            .Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();
        services.AddSingleton(options);

        if (!string.IsNullOrWhiteSpace(options.Url))
        {
            // Gerçek Elasticsearch: tek istemci (singleton) — ES 8.x tavsiye.
            var settings = new ElasticsearchClientSettings(new Uri(options.Url))
                .DefaultIndex(options.IndexName);
            services.AddSingleton(new ElasticsearchClient(settings));
            services.AddSingleton<ISearchIndex, ElasticsearchSearchIndex>();
        }
        else
        {
            // ES yok → bellek-içi indeks (singleton: consumer yazımı ile arama okuması aynı store).
            services.AddSingleton<ISearchIndex, InMemorySearchIndex>();
        }

        return services;
    }

    /// <summary>
    /// Gerçek ES kullanılıyorsa indeks/mapping'i başlangıçta idempotent oluşturur (docs/06 S2.3).
    /// InMemory'de no-op. Program.cs açılışta bir kez çağırır.
    /// </summary>
    public static async Task EnsureSearchIndexAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var index = services.GetRequiredService<ISearchIndex>();
        if (index is ElasticsearchSearchIndex elastic)
        {
            var logger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("HalOS.Search.Infrastructure.EnsureIndex");
            logger.LogInformation("Elasticsearch arama indeksi hazırlanıyor.");
            await elastic.EnsureIndexAsync(cancellationToken);
        }
    }
}

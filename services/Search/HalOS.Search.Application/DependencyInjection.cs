using HalOS.Search.Application.Search;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Search.Application;

/// <summary>
/// Search.Application kompozisyon kökü (docs/07 §2). Arama sorgu işleyicisini kaydeder. Consumer'lar
/// MassTransit registrasyonuyla (Infrastructure/Api) eklenir; ISearchIndex uygulaması Infrastructure'da
/// seçilir (ES varsa Elasticsearch, yoksa InMemory).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSearchApplication(this IServiceCollection services)
    {
        services.AddScoped<SearchQueryHandler>();
        return services;
    }
}

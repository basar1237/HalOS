using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Infrastructure.Gateways;
using HalOS.Integration.Infrastructure.Persistence;
using HalOS.Integration.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Integration.Infrastructure;

/// <summary>
/// Infrastructure katmanının DI kaydı: EF Core (Npgsql), repository'ler, müstahsil profil okuma/yazma
/// portları, GİB e-belge gateway (bu slice STUB), outbox (SaveChanges içinde). Finance/Sales deseniyle
/// birebir.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIntegrationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IntegrationDb");

        services.AddDbContext<IntegrationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "integration"));
            // Tenant izolasyonu (BK-8) dinamik closure filtresi ile: filtre her sorguda
            // ITenantContext.TenantId'yi canlı okur → tek model tüm tenant'lar için doğru.
        });

        // Domain event'leri outbox'a IntegrationDbContext.SaveChangesAsync içinde tenant'lı yazılır
        // (docs/04 §10); ayrı bir IOutboxWriter yolu yok — Finance/Sales/Party ile aynı desen.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IntegrationDbContext>());

        services.AddScoped<IProducerReceiptRepository, ProducerReceiptRepository>();
        services.AddScoped<IProducerTaxProfileReader, ProducerTaxProfileReader>();
        services.AddScoped<IProducerTaxProfileWriter, ProducerTaxProfileWriter>();

        // GİB e-belge gönderimi — bu slice STUB (gerçek sandbox entegrasyonu sonraki slice, ADR-007).
        services.AddScoped<IEDocumentGateway, StubEDocumentGateway>();

        return services;
    }
}

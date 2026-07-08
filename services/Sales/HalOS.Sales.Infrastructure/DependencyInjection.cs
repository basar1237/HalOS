using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Infrastructure;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Infrastructure.Persistence;
using HalOS.Sales.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Sales.Infrastructure;

/// <summary>Infrastructure katmanının DI kaydı: EF Core (Npgsql), repository'ler, outbox (SaveChanges içinde).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSalesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SalesDb");

        services.AddDbContext<SalesDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "sales"));
            // Tenant izolasyonu (BK-8) dinamik closure filtresi ile: filtre her sorguda
            // ITenantContext.TenantId'yi canlı okur → tek model tüm tenant'lar için doğru.
        });

        // Domain event'leri outbox'a SalesDbContext.SaveChangesAsync içinde tenant'lı yazılır
        // (docs/04 §10); ayrı bir IOutboxWriter yolu yok — Identity/Party ile aynı desen.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SalesDbContext>());

        // Denetim kaydı (audit_log) sink'i: komut sonrası kim/ne/ne zaman'ı SalesDbContext'e yazar
        // (docs/05 §3.11). AuditLoggingBehavior bunu kullanır; outbox deseniyle paralel.
        services.AddScoped<IAuditLogSink, AuditLogSink<SalesDbContext>>();

        services.AddScoped<IConsignmentRepository, ConsignmentRepository>();
        services.AddScoped<ISaleTransactionRepository, SaleTransactionRepository>();

        // Müstahsile-özel oran okuma modeli (Party senkronu); IRateProvider bunu config'e tercih eder.
        services.AddScoped<IProducerRateProfileReader, ProducerRateProfileReader>();

        return services;
    }
}

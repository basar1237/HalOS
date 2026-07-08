using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Infrastructure;
using HalOS.Party.Application.Abstractions;
using HalOS.Party.Infrastructure.Persistence;
using HalOS.Party.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Party.Infrastructure;

/// <summary>Infrastructure katmanının DI kaydı: EF Core (Npgsql), repository, outbox (SaveChanges içinde).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPartyInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PartyDb");

        services.AddDbContext<PartyDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "party"));
            // Tenant izolasyonu (BK-8) dinamik closure filtresi ile: filtre her sorguda
            // ITenantContext.TenantId'yi canlı okur → tek model tüm tenant'lar için doğru.
        });

        // Domain event'leri outbox'a PartyDbContext.SaveChangesAsync içinde tenant'lı yazılır
        // (docs/04 §10); ayrı bir IOutboxWriter yolu yok — Identity servisiyle aynı desen.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PartyDbContext>());

        // Denetim kaydı (audit_log) sink'i: komut sonrası kim/ne/ne zaman'ı PartyDbContext'e yazar
        // (docs/05 §3.11). AuditLoggingBehavior bunu kullanır; outbox deseniyle paralel.
        services.AddScoped<IAuditLogSink, AuditLogSink<PartyDbContext>>();

        services.AddScoped<IPartyRepository, PartyRepository>();

        return services;
    }
}

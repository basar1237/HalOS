using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Infrastructure;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Infrastructure.Persistence;
using HalOS.Finance.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Finance.Infrastructure;

/// <summary>Infrastructure katmanının DI kaydı: EF Core (Npgsql), repository'ler, outbox (SaveChanges içinde).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFinanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FinanceDb");

        services.AddDbContext<FinanceDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "finance"));
            // Tenant izolasyonu (BK-8) dinamik closure filtresi ile: filtre her sorguda
            // ITenantContext.TenantId'yi canlı okur → tek model tüm tenant'lar için doğru.
        });

        // Domain event'leri outbox'a FinanceDbContext.SaveChangesAsync içinde tenant'lı yazılır
        // (docs/04 §10); ayrı bir IOutboxWriter yolu yok — Sales/Party ile aynı desen.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FinanceDbContext>());

        // Denetim kaydı (audit_log) sink'i: komut sonrası kim/ne/ne zaman'ı FinanceDbContext'e yazar
        // (docs/05 §3.11). AuditLoggingBehavior bunu kullanır; outbox deseniyle paralel.
        services.AddScoped<IAuditLogSink, AuditLogSink<FinanceDbContext>>();

        services.AddScoped<ICurrentAccountRepository, CurrentAccountRepository>();

        return services;
    }
}

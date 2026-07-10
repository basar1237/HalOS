using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Infrastructure.Persistence;
using HalOS.ColdChain.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.ColdChain.Infrastructure;

/// <summary>
/// Infrastructure katmanının DI kaydı: EF Core (Npgsql), repository, outbox (SaveChanges içinde).
/// NOT: Telemetri servisi olduğundan audit_log sink'i KAYDEDİLMEZ (bkz Application.DependencyInjection).
/// Inventory deseniyle birebir (audit hariç).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddColdChainInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ColdChainDb");

        services.AddDbContext<ColdChainDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "coldchain"));
        });

        // Domain event'leri (TemperatureThresholdBreached) outbox'a ColdChainDbContext.SaveChangesAsync
        // içinde tenant'lı yazılır (docs/04 §10); Inventory ile aynı desen.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ColdChainDbContext>());

        services.AddScoped<IColdStorageUnitRepository, ColdStorageUnitRepository>();

        return services;
    }
}

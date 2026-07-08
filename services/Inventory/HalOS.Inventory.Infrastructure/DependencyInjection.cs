using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Infrastructure;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Infrastructure.Persistence;
using HalOS.Inventory.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Inventory.Infrastructure;

/// <summary>Infrastructure katmanının DI kaydı: EF Core (Npgsql), repository'ler, outbox (SaveChanges içinde).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InventoryDb");

        services.AddDbContext<InventoryDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "inventory"));
            // Tenant izolasyonu (BK-8) dinamik closure filtresi ile: filtre her sorguda
            // ITenantContext.TenantId'yi canlı okur → tek model tüm tenant'lar için doğru.
        });

        // Domain event'leri (SpoilageRecorded) outbox'a InventoryDbContext.SaveChangesAsync içinde
        // tenant'lı yazılır (docs/04 §10); ayrı bir IOutboxWriter yolu yok — Finance ile aynı desen.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InventoryDbContext>());

        // Denetim kaydı (audit_log) sink'i: komut sonrası kim/ne/ne zaman'ı InventoryDbContext'e
        // yazar (docs/05 §3.11). AuditLoggingBehavior bunu kullanır; outbox deseniyle paralel.
        services.AddScoped<IAuditLogSink, AuditLogSink<InventoryDbContext>>();

        services.AddScoped<IStockItemRepository, StockItemRepository>();

        // Depo lokasyonu (docs/06 S2.1): warehouse repo + varsayılan depo sağlayıcısı. Provider,
        // olay-güdümlü giriş/çıkışta varsayılan depoyu getirir/oluşturur (Consumer içinde HTTP yok).
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IWarehouseProvider, WarehouseProvider>();

        return services;
    }
}

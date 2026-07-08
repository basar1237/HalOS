using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Infrastructure;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Infrastructure.Authentication;
using HalOS.Identity.Infrastructure.Messaging;
using HalOS.Identity.Infrastructure.Persistence;
using HalOS.Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Identity.Infrastructure;

/// <summary>Infrastructure katmanının DI kaydı: EF Core, repository'ler, JWT/2FA/parola servisleri, outbox.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var connectionString = configuration.GetConnectionString("IdentityDb");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"));
            // Tenant izolasyonu (BK-8) dinamik closure filtresi ile sağlanır: filtre her
            // sorguda ITenantContext.TenantId'yi canlı okur, dolayısıyla tek model tüm
            // tenant'lar için doğrudur. Model önbellek anahtarını tenant'a göre çeşitlendirmeye
            // gerek yoktur (10k tenant'ta model cache'i şişirmez).
        });

        // Domain event'leri outbox'a IdentityDbContext.SaveChangesAsync içinde tenant'lı
        // yazılır (docs/04 §10); ayrı bir IOutboxWriter yolu yok — tek yol, tek doğruluk.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        // Denetim kaydı (audit_log) sink'i: komut sonrası kim/ne/ne zaman'ı IdentityDbContext'e
        // yazar (docs/05 §3.11). AuditLoggingBehavior bunu kullanır; outbox deseniyle paralel.
        services.AddScoped<IAuditLogSink, AuditLogSink<IdentityDbContext>>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddScoped<ITokenService, TokenService>();

        // RabbitMQ yayıncısı arayüz arkasında; şimdilik no-op (docs/06 S0.5).
        services.AddSingleton<IEventPublisher, NoOpEventPublisher>();

        return services;
    }
}

using System.Reflection;
using FluentValidation;
using HalOS.BuildingBlocks.Application;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.ColdChain.Application;

/// <summary>
/// Application katmanının DI kaydı: MediatR handler'ları, validator'lar ve validasyon pipeline'ı
/// (docs/07 §5). NOT: Soğuk zincir telemetrisi (sensör okumaları) YÜKSEK HACİMLİdir → her okuma
/// bir kullanıcı işlemi değildir; bu yüzden diğer servislerdeki <c>AuditLoggingBehavior</c> BURADA
/// KASITEN kaydedilmez (audit_log'u telemetriyle boğmamak için). Yalnız girdi doğrulaması çalışır.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddColdChainApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}

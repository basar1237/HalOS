using System.Reflection;
using FluentValidation;
using HalOS.BuildingBlocks.Application;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Integration.Application;

/// <summary>
/// Application katmanının DI kaydı: MediatR handler'ları, validator'lar ve validasyon pipeline'ı
/// (docs/07 §5). SaleCompletedConsumer + ProducerWithholdingProfileChangedConsumer bu assembly'de
/// yaşar ancak MassTransit registrasyonuna API kompozisyon kökünde <c>AddHalOSMessaging</c> ile
/// eklenir (Program.cs). Finance deseniyle birebir.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIntegrationApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Validasyon pipeline'ı handler'dan önce çalışır (docs/07 §5).
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            // Denetim (audit_log): her komut için kim/ne/ne zaman yazılır (docs/05 §3.11).
            // Validasyondan SONRA; yalnız komutları denetler, query'leri denetlemez.
            cfg.AddOpenBehavior(typeof(AuditLoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}

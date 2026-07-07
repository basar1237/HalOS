using System.Reflection;
using FluentValidation;
using HalOS.BuildingBlocks.Application;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Finance.Application;

/// <summary>
/// Application katmanının DI kaydı: MediatR handler'ları, validator'lar ve validasyon pipeline'ı
/// (docs/07 §5). SaleCompletedConsumer bu assembly'de yaşar ancak MassTransit registrasyonuna
/// API kompozisyon kökünde <c>AddHalOSMessaging</c> ile eklenir (Program.cs). Sales deseniyle birebir.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFinanceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Validasyon pipeline'ı handler'dan önce çalışır (docs/07 §5).
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}

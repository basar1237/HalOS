using System.Reflection;
using FluentValidation;
using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Rates;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Sales.Application;

/// <summary>
/// Application katmanının DI kaydı: MediatR handler'ları, validator'lar, validasyon pipeline'ı
/// ve config-tabanlı <see cref="DefaultRateProvider"/> (docs/07 §5; docs/02 §4).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSalesApplication(
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

        // Kesinti oranları config'ten (docs/02 §4; sihirli sabit yerine config — docs/07 §10).
        services.Configure<RateOptions>(configuration.GetSection(RateOptions.SectionName));
        services.AddScoped<IRateProvider, DefaultRateProvider>();

        return services;
    }
}

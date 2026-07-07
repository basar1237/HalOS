using System.Reflection;
using FluentValidation;
using HalOS.BuildingBlocks.Application;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Identity.Application;

/// <summary>Application katmanının DI kaydı: MediatR handler'ları, validator'lar ve validasyon pipeline'ı.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
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

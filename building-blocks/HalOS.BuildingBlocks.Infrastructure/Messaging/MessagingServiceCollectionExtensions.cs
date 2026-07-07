using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Servislerin mesajlaşma altyapısını tek çağrıda kuran DI yardımcısı (docs/04 ADR-006 / §10):
/// MassTransit + RabbitMQ taşıması, el-yapımı outbox'ı yayınlayan <see cref="OutboxDispatcher{TContext}"/>,
/// <see cref="IEventPublisher"/> ve consumer tenant bağlamı (<see cref="AmbientTenantContext"/> +
/// <see cref="TenantConsumeFilter{T}"/>). MassTransit'in KENDİ outbox'ı açılmaz.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Verilen <typeparamref name="TContext"/> için mesajlaşmayı kaydeder. Tüketiciler
    /// (<paramref name="configureConsumers"/>) MassTransit registrasyonuna eklenir; yoksa servis
    /// yalnız yayıncı (publisher) + dispatcher rolündedir.
    /// </summary>
    /// <typeparam name="TContext">Servisin tenant-kapsamlı DbContext'i (outbox burada tutulur).</typeparam>
    public static IServiceCollection AddHalOSMessaging<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
        where TContext : TenantDbContextBase
    {
        var outboxOptions = configuration.GetSection(OutboxDispatcherOptions.SectionName)
            .Get<OutboxDispatcherOptions>() ?? new OutboxDispatcherOptions();
        services.AddSingleton(outboxOptions);

        var rabbit = configuration.GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        // Broker mesajından tenant taşıyan consumer scope bağlamı (TenantConsumeFilter doldurur).
        services.AddScoped<AmbientTenantContext>();

        services.AddMassTransit(x =>
        {
            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbit.Host, rabbit.VirtualHost, h =>
                {
                    h.Username(rabbit.Username);
                    h.Password(rabbit.Password);
                });

                // Gelen her mesajda tenant'ı event'ten (ITenantScopedEvent) çözüp ambient bağlama
                // yaz — SaveChanges öncesi doğru tenant izolasyonu (docs/07 §6 / BK-8).
                cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);

                cfg.ConfigureEndpoints(context);
            });
        });

        // Domain event'lerini bus'a yayınlayan soyutlama (IPublishEndpoint'e bağlı → scoped).
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        // El-yapımı outbox'ı poll edip yayınlayan arka plan servisi.
        services.AddHostedService<OutboxDispatcher<TContext>>();

        return services;
    }
}

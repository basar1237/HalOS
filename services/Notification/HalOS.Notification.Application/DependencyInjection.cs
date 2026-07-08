using Microsoft.Extensions.DependencyInjection;

namespace HalOS.Notification.Application;

/// <summary>
/// Notification.Application kompozisyon kökü (docs/07 §2). Şu an yalnız consumer'lar vardır ve onlar
/// MassTransit registrasyonuyla (Api) eklenir; <c>IDashboardBroadcaster</c> uygulaması (SignalR)
/// Api katmanında kaydedilir. Metot ileride uygulama-içi servisler için ayrılmıştır.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        return services;
    }
}

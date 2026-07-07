using HalOS.BuildingBlocks.Contracts;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace HalOS.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Consume pipeline filtresi: gelen mesaj <see cref="ITenantScopedEvent"/> ise, mesajın taşıdığı
/// <see cref="ITenantScopedEvent.TenantId"/>'yi bu consume scope'undaki
/// <see cref="AmbientTenantContext"/>'e set eder. Böylece consumer'ın açtığı
/// <c>TenantDbContextBase</c> global query filter'ı DOĞRU tenant'ta çalışır ve tüm okuma/yazma
/// (<c>SaveChanges</c> dahil) o tenant kapsamında izole kalır (docs/07 §6 / BK-8).
///
/// Broker üzerinden geçen mesajda HTTP/JWT bağlamı olmadığından tenant, event'in kendisiyle
/// taşınır (docs/04 §10). Tenant taşımayan mesajlar için filtre etkisizdir (sadece devam eder).
/// </summary>
/// <typeparam name="T">Consume edilen mesaj tipi.</typeparam>
public sealed class TenantConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    public void Probe(ProbeContext context) => context.CreateFilterScope("tenantScope");

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (context.Message is ITenantScopedEvent tenantScoped)
        {
            // Consume scope'undaki DI sağlayıcısından ambient tenant bağlamını al ve doldur.
            // AmbientTenantContext consumer scope'unda ITenantContext olarak kayıtlıdır.
            var ambient = context.GetPayload<IServiceProvider>()
                .GetService<AmbientTenantContext>();

            ambient?.SetTenant(tenantScoped.TenantId);
        }

        await next.Send(context);
    }
}

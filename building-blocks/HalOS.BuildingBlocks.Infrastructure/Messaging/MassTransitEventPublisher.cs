using HalOS.BuildingBlocks.Domain;
using MassTransit;

namespace HalOS.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// <see cref="IEventPublisher"/>'ın MassTransit uygulaması. Event'i <see cref="IPublishEndpoint"/>
/// ile yayınlar. KRİTİK: yayın <b>çalışma-zamanı (runtime)</b> tipiyle yapılır — statik
/// <see cref="IDomainEvent"/> tipiyle değil. Aksi halde MassTransit mesajı <c>IDomainEvent</c>
/// sözleşmesiyle yayınlar ve tüketiciler somut sözleşme tipini (ör. <c>SaleCompleted</c>)
/// alamaz. Bu yüzden <see cref="IPublishEndpoint.Publish(object, Type, CancellationToken)"/>
/// aşırı yüklemesi kullanılır (docs/04 §10).
/// </summary>
public sealed class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync(IDomainEvent evt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        // Mesajı somut runtime tipiyle yayınla ki tüketiciler doğru sözleşme tipini dinlesin.
        return _publishEndpoint.Publish(evt, evt.GetType(), cancellationToken);
    }
}

using HalOS.BuildingBlocks.Domain;

namespace HalOS.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Domain event'lerini asenkron event bus'a (RabbitMQ, docs/04 ADR-006) yayınlayan soyutlama.
/// Handler'lar/aggregate'ler doğrudan yayın yapmaz (docs/07 §5): önce el-yapımı transactional
/// outbox'a atomik yazılır, ardından <see cref="OutboxDispatcher{TContext}"/> bu arayüzle
/// yayınlar (docs/04 §10). Uygulama, event'i <b>runtime</b> tipiyle yayınlar ki tüketiciler
/// somut sözleşme (ör. <c>SaleCompleted</c>) tipiyle dinleyebilsin.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Verilen domain event'ini bus'a yayınlar. <paramref name="evt"/> statik olarak
    /// <see cref="IDomainEvent"/> görünse de yayın çalışma-zamanı tipiyle yapılmalıdır.
    /// </summary>
    Task PublishAsync(IDomainEvent evt, CancellationToken cancellationToken = default);
}

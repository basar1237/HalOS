namespace HalOS.Identity.Application.Abstractions;

/// <summary>
/// Servisler arası mesajlaşma (RabbitMQ) yayıncısı port'u (docs/04 ADR-006, §10).
/// Doğrudan yayın yerine outbox tercih edilir (docs/07 §5); bu port outbox işlemcisinin
/// arkasında yaşar. Bu fazda somut bağlantı testte gerekmez — no-op/in-memory impl kullanılır.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(string type, string payload, CancellationToken cancellationToken = default);
}

using HalOS.Identity.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace HalOS.Identity.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ yayıncısının yer tutucu (no-op) implementasyonu (docs/06 S0.5). Gerçek RabbitMQ
/// bağlantısı bu arayüz arkasına eklenecek; bu fazda dış bağlantı testte gerekmesin diye
/// yalnızca loglar. Outbox işlemcisi ileride bu port üzerinden yayın yapacak.
/// </summary>
internal sealed class NoOpEventPublisher : IEventPublisher
{
    private readonly ILogger<NoOpEventPublisher> _logger;

    public NoOpEventPublisher(ILogger<NoOpEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string type, string payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Event yayını (no-op): {EventType}. Gerçek RabbitMQ yayıncısı henüz bağlanmadı.",
            type);
        return Task.CompletedTask;
    }
}

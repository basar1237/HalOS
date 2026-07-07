using System.Collections.Concurrent;
using System.Text.Json;
using HalOS.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HalOS.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// El-yapımı transactional outbox'ın DAĞITICISI (dispatcher). Arka planda periyodik olarak
/// <typeparamref name="TContext"/> üzerinden henüz işlenmemiş (<c>ProcessedOnUtc == null</c>)
/// <see cref="OutboxMessage"/>'ları oluşma sırasına göre çeker, her mesajın <c>Type</c>
/// string'inden CLR tipini çözer, <c>Content</c>'i deserialize eder ve
/// <see cref="IEventPublisher"/> ile bus'a yayınlar. Başarıda <c>ProcessedOnUtc</c> damgalanır;
/// hatada <c>Error</c> doldurulur ve mesaj işlenmemiş kalır (bir sonraki turda TEKRAR denenir) —
/// böylece en-az-bir-kez teslim garantisi korunur (docs/04 §10).
///
/// MassTransit'in kendi outbox'ı KAPALI kalır; bu el-yapımı akış tek gerçek kaynak olarak
/// KORUNUR (görev kuralı). Poll aralığı ve batch boyutu <see cref="OutboxDispatcherOptions"/>
/// ile ayarlanabilir.
/// </summary>
/// <typeparam name="TContext">Servisin tenant-kapsamlı DbContext'i.</typeparam>
public sealed class OutboxDispatcher<TContext> : BackgroundService
    where TContext : TenantDbContextBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxDispatcherOptions _options;
    private readonly ILogger<OutboxDispatcher<TContext>> _logger;

    // Type string -> CLR tipi çözümü maliyetlidir; başarılı çözümler önbelleğe alınır.
    private static readonly ConcurrentDictionary<string, Type?> TypeCache = new();

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        OutboxDispatcherOptions options,
        ILogger<OutboxDispatcher<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Uygulama kapanıyor — döngüden sessizce çık.
                break;
            }
            catch (Exception ex)
            {
                // ExecuteAsync ASLA dışarı exception atmamalı; hata yutulur, döngü devam eder.
                _logger.LogError(ex, "Outbox dispatch döngüsü beklenmeyen hata ile karşılaştı.");
            }

            try
            {
                await Task.Delay(_options.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        // IEventPublisher scoped'tur (IPublishEndpoint'e bağlı); singleton dispatcher onu
        // constructor'da tutamaz — her turda oluşturulan scope'tan çözülür (captive dependency yok).
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        // Bekleyen mesajları oluşma sırasına göre batch olarak çek. IgnoreQueryFilters:
        // OutboxMessage tenant-owned DEĞİL (nullable tenant_id) ama dispatcher tüm tenant'ların
        // mesajlarını yayınlamalı; global filter'a takılmamak için güvenli tarafta kalınır.
        var pending = await context.Set<OutboxMessage>()
            .IgnoreQueryFilters()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            // Her mesaj kendi try/catch'i ile — biri patlarsa diğerleri etkilenmez.
            try
            {
                var evt = Deserialize(message);
                await publisher.PublishAsync(evt, cancellationToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                // Hata mesaja yazılır; ProcessedOnUtc null kalır → sonraki turda tekrar denenir.
                message.Error = ex.Message;
                _logger.LogError(
                    ex,
                    "Outbox mesajı {OutboxMessageId} ({OutboxMessageType}) yayınlanamadı; tekrar denenecek.",
                    message.Id,
                    message.Type);
            }
        }

        // Başarılı damgalar ve hata alanları aynı SaveChanges ile kalıcılaşır.
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IDomainEvent Deserialize(OutboxMessage message)
    {
        var clrType = ResolveType(message.Type)
            ?? throw new InvalidOperationException(
                $"Outbox mesajının tipi çözülemedi: '{message.Type}'. Sözleşme assembly'si yüklü mü?");

        var deserialized = JsonSerializer.Deserialize(message.Content, clrType)
            ?? throw new InvalidOperationException(
                $"Outbox mesaj içeriği deserialize edilemedi (tip: '{message.Type}').");

        if (deserialized is not IDomainEvent domainEvent)
        {
            throw new InvalidOperationException(
                $"Çözülen tip bir IDomainEvent değil: '{message.Type}'.");
        }

        return domainEvent;
    }

    /// <summary>
    /// Type string'inden (genellikle <c>Type.FullName</c>) CLR tipini çözer. Önce doğrudan
    /// <see cref="Type.GetType(string)"/>, ardından yüklü tüm assembly'lerde (Contracts dahil)
    /// tam ad eşleşmesiyle arar. Sonuç (null dahil) önbelleğe alınır.
    /// </summary>
    private static Type? ResolveType(string typeName) =>
        TypeCache.GetOrAdd(typeName, static name =>
        {
            var direct = Type.GetType(name, throwOnError: false);
            if (direct is not null)
            {
                return direct;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var found = assembly.GetType(name, throwOnError: false);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        });
}

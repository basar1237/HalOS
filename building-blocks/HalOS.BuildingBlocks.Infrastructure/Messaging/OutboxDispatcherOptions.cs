namespace HalOS.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// <see cref="OutboxDispatcher{TContext}"/> için yapılandırma. Poll aralığı ve batch boyutu
/// ayarlanabilir (docs/04 §10). Varsayılanlar servisler-arası tipik gecikme/yük dengesi için
/// seçilmiştir; ihtiyaca göre <c>appsettings.json</c> ("Outbox" bölümü) üzerinden değiştirilebilir.
/// </summary>
public sealed class OutboxDispatcherOptions
{
    /// <summary>Yapılandırma bölümü adı ("Outbox").</summary>
    public const string SectionName = "Outbox";

    /// <summary>Dispatch döngüleri arası bekleme (varsayılan ~2 sn).</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Her turda çekilecek en fazla bekleyen mesaj sayısı (varsayılan 50).</summary>
    public int BatchSize { get; set; } = 50;
}

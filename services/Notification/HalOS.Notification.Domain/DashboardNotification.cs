namespace HalOS.Notification.Domain;

/// <summary>
/// Canlı dashboard'a (patron/yönetici ekranı) yayınlanan tek bir bildirim (docs/06 S2.2 canlı SignalR
/// dashboard; docs/02 Bildirim bağlamı: <c>SaleCompleted</c> → patrona canlı özet). Servisler-arası
/// event (ör. <c>SaleCompleted</c>) consumer içinde bu modele çevrilir ve
/// <c>IDashboardBroadcaster</c> ile YALNIZ ilgili tenant grubuna gönderilir (BK-8).
///
/// Kalıcı değildir: Notification servisi DB tutmaz (Postgres/outbox YOK) — salt tüketici→broadcast.
/// SignalR üzerinden istemciye JSON olarak (payload alanı esnek) taşınır.
/// </summary>
/// <param name="Type">Bildirim türü/kanonik olay adı (ör. <c>"sale.completed"</c>). İstemci ikon/renk
/// seçimi ve filtreleme için kullanır.</param>
/// <param name="TenantId">Bildirimin ait olduğu kiracı. Broadcast bu tenant'ın grubuna (<c>tenant-{id}</c>)
/// kısıtlanır; başka tenant asla almaz (BK-8, çapraz-tenant sızıntısı YASAK).</param>
/// <param name="Title">Kısa başlık (ör. <c>"Yeni satış"</c>).</param>
/// <param name="Message">İnsan-okunur özet (ör. <c>"Yeni satış: 1.100,50 TL net, 1.250,50 brüt"</c>).</param>
/// <param name="Payload">İstemcinin derinleşmesi için serbest biçimli ek veri (ör. satış kimliği,
/// tutarlar). Kalıcı şema yok; salt taşımadır.</param>
/// <param name="OccurredOnUtc">Kaynak olayın gerçekleştiği an (UTC). Kaynak event'ten taşınır.</param>
public sealed record DashboardNotification(
    string Type,
    Guid TenantId,
    string Title,
    string Message,
    IReadOnlyDictionary<string, object?> Payload,
    DateTime OccurredOnUtc);

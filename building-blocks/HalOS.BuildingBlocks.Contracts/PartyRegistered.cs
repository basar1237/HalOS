using HalOS.BuildingBlocks.Domain;

namespace HalOS.BuildingBlocks.Contracts;

/// <summary>
/// Yeni bir taraf (cari kart) kaydedildiğinde yayınlanır (docs/02 §6 katalog deseni). Servisler-arası
/// entegrasyon event'idir; bu yüzden paylaşılan <c>Contracts</c> projesinde yaşar (docs/04 ADR-006,
/// docs/07 §2) — böylece hem kaynak (Party) hem tüketen servisler (ör. Search okuma modeli, docs/06
/// S2.3) döngüsel bağımlılık olmadan referans verebilir. Event adı PascalCase geçmiş zaman (docs/07 §3).
///
/// <see cref="ITenantScopedEvent"/>'i uygular: broker üzerinden geçerken tenant bağlamı mesajın
/// kendisiyle taşınır, consumer <see cref="TenantId"/>'yi ambient tenant'a set eder (docs/07 §6 / BK-8).
///
/// <para>
/// Arama okuma modeli (docs/06 S2.3 — "Ali'nin her şeyini 1 sn'de") için gereken alanlar taşınır:
/// görünen ad, kimlik numarası (TCKN/VKN) ve taraf rol(ler)i. Search servisi bu event'i tüketip
/// <c>PartySearchDocument</c> indeksler; kaynak servisin (Party) DB'sine DOKUNMAZ — CQRS ayrı
/// okuma modeli (docs/04 ADR-007). Kimlik/rol alanları consumer'ın tekil sorgu yapmadan tam bir
/// arama dokümanı kurabilmesi için event'le taşınır (docs/07 §5, docs/04 §10 event-taşımalı entegrasyon).
/// </para>
/// </summary>
/// <param name="PartyId">Kaydedilen tarafın kimliği (<c>Party.Id</c>).</param>
/// <param name="TenantId">Tarafın ait olduğu kiracı (tenant) kimliği (docs/07 §6 / BK-8).</param>
/// <param name="DisplayName">Görünen ad (docs/05 <c>display_name</c>). Arama sonucunun ana etiketidir.</param>
/// <param name="TaxNumber">Kimlik numarası — TCKN (11 hane, bireysel/müstahsil) ya da VKN (10 hane, tüzel);
/// hiçbiri yoksa null (docs/05 <c>tckn</c>/<c>vkn</c>). Arama bu alan üzerinde de eşleşir ("Ali'yi VKN ile bul").</param>
/// <param name="PartyType">Taraf rol(ler)i, virgülle ayrılmış metin (ör. "Producer,Buyer") — enum değil metin,
/// çünkü Contracts assembly'si servis domain'ine (PartyRoleType) bağlanamaz (docs/07 §2). Arama filtresi/
/// gösterimi için kullanılır.</param>
/// <param name="OccurredOnUtc">Event'in oluştuğu an (UTC).</param>
public sealed record PartyRegistered(
    Guid PartyId,
    Guid TenantId,
    string DisplayName,
    string? TaxNumber,
    string PartyType,
    DateTime OccurredOnUtc) : IDomainEvent, ITenantScopedEvent;

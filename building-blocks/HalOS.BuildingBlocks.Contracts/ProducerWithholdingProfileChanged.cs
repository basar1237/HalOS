using HalOS.BuildingBlocks.Domain;

namespace HalOS.BuildingBlocks.Contracts;

/// <summary>
/// Bir müstahsilin (Producer) stopaj/kesinti oran profili ilk kez set edildiğinde veya
/// değiştiğinde Party servisi tarafından yayınlanır (docs/02 §6: Party → Sales oran senkronu).
/// Sales servisi bu event ile müstahsile-özel oranları kendi okuma modeline (ProducerRateProfile)
/// yazar; böylece satış tamamlanırken hakediş, müstahsilin gerçek oranlarıyla hesaplanır
/// (tenant config'e düşmeden — <c>IRateProvider</c>). Çekirdek servisler-arası entegrasyon
/// event'i olduğundan paylaşılan <c>Contracts</c> projesinde yaşar. Event adı PascalCase
/// geçmiş zaman (docs/07 §3).
///
/// Alanlar Party aggregate'inin <c>WithholdingProfile</c> value object'inde GERÇEKTEN var olan
/// oranlarla sınırlıdır: zirai stopaj ve çiftçi Bağ-Kur (docs/02 §1.3). Komisyon oranı Party'de
/// tutulmadığından bu event'te de taşınmaz; komisyon tenant config'ten çözülür (docs/02 §4).
///
/// <see cref="ITenantScopedEvent"/>'i uygular: broker üzerinden geçerken tenant bağlamı mesajın
/// kendisiyle taşınır, consumer <see cref="TenantId"/>'yi ambient tenant'a set eder
/// (docs/07 §6 / BK-8).
/// </summary>
public sealed record ProducerWithholdingProfileChanged(
    Guid TenantId,
    Guid ProducerPartyId,
    decimal AgriWithholdingRate,
    decimal FarmerSskRate,
    DateTime OccurredOnUtc) : IDomainEvent, ITenantScopedEvent;

using HalOS.BuildingBlocks.Domain;

namespace HalOS.BuildingBlocks.Contracts;

/// <summary>
/// Bir müstahsil (Producer rolü olan Party) oluşturulduğunda VE güncellendiğinde Party servisi
/// tarafından yayınlanır (docs/02 §6: Party → Sales oran senkronu; docs/02 §1.3 + BK-4 e-MM).
/// Yalnız oran değişiminde değil, her müstahsil kaydı için raise edilir: hem Sales oran okuma
/// modelini (ProducerRateProfile) senkronlar hem de Integration servisinin e-müstahsil makbuzu
/// (e-MM) kararı için müstahsilin <see cref="KeepsRecords"/> bilgisine ihtiyacı vardır — e-MM
/// yalnızca kayıt TUTMAYAN müstahsil için üretilir (BK-4). Çekirdek servisler-arası entegrasyon
/// event'i olduğundan paylaşılan <c>Contracts</c> projesinde yaşar. Event adı PascalCase
/// geçmiş zaman (docs/07 §3).
///
/// Oran alanları Party aggregate'inin <c>WithholdingProfile</c> value object'inde GERÇEKTEN var
/// olan oranlarla sınırlıdır: zirai stopaj ve çiftçi Bağ-Kur (docs/02 §1.3). Komisyon oranı
/// Party'de tutulmadığından bu event'te de taşınmaz; komisyon tenant config'ten çözülür (docs/02 §4).
///
/// <see cref="ITenantScopedEvent"/>'i uygular: broker üzerinden geçerken tenant bağlamı mesajın
/// kendisiyle taşınır, consumer <see cref="TenantId"/>'yi ambient tenant'a set eder
/// (docs/07 §6 / BK-8).
/// </summary>
/// <param name="TenantId">Müstahsilin bağlı olduğu işletme (tenant) — ITenantScopedEvent (BK-8).</param>
/// <param name="ProducerPartyId">Müstahsil Party kimliği.</param>
/// <param name="AgriWithholdingRate">Zirai stopaj oranı (docs/02 §1.3).</param>
/// <param name="FarmerSskRate">Çiftçi Bağ-Kur (SGK) primi oranı (docs/02 §1.3).</param>
/// <param name="KeepsRecords">
/// Müstahsil defter/kayıt tutuyor mu — e-MM gerekliliğini belirler. e-müstahsil makbuzu yalnız
/// kayıt TUTMAYAN müstahsil için üretilir (docs/05 §3.2, BK-4). Integration servisi bu bilgiyle
/// e-belge üretimine karar verir; Sales bu alanı kullanmaz.
/// </param>
/// <param name="OccurredOnUtc">Olayın gerçekleştiği an (UTC) — sıra-dışı teslimatta monoton uygulama için.</param>
public sealed record ProducerWithholdingProfileChanged(
    Guid TenantId,
    Guid ProducerPartyId,
    decimal AgriWithholdingRate,
    decimal FarmerSskRate,
    bool KeepsRecords,
    DateTime OccurredOnUtc) : IDomainEvent, ITenantScopedEvent;

using HalOS.BuildingBlocks.Domain;

namespace HalOS.BuildingBlocks.Contracts;

/// <summary>
/// Bir soğuk hava deposunun (ColdStorageUnit) sıcaklık okuması izin verilen aralığın DIŞINA
/// çıktığında yayınlanır (docs/04 §6 IoT/Soğuk Zincir: eşik aşımı → domain event → anlık bildirim +
/// AI fire tahmini; docs/06 S3.1). Çekirdek servisler-arası entegrasyon event'idir → paylaşılan
/// <c>Contracts</c> projesinde yaşar. Notification servisi bunu tüketip patronun canlı dashboard'una
/// alarm yayınlar (Sales <see cref="SaleCompleted"/> deseniyle birebir).
///
/// <see cref="ITenantScopedEvent"/>'i uygular: broker üzerinden geçerken tenant bağlamı mesajın
/// kendisiyle taşınır, consumer <see cref="TenantId"/>'yi ambient tenant'a set eder (docs/07 §6 / BK-8).
/// Event adı PascalCase geçmiş zaman (docs/07 §3).
/// </summary>
/// <param name="ColdStorageUnitId">İhlalin gerçekleştiği soğuk hava deposunun kimliği.</param>
/// <param name="TenantId">Event'in ait olduğu kiracı (tenant).</param>
/// <param name="UnitName">Deponun okunur adı (bildirim metni için; consumer kaynak DB'ye sorgu yapmaz).</param>
/// <param name="TemperatureC">Ölçülen sıcaklık (santigrat, NUMERIC(6,2); decimal — asla float, BK-2).</param>
/// <param name="MinTempC">İzin verilen alt sıcaklık eşiği.</param>
/// <param name="MaxTempC">İzin verilen üst sıcaklık eşiği.</param>
/// <param name="AboveMax">true → üst eşik aşıldı (çok sıcak); false → alt eşiğin altına inildi (çok soğuk).</param>
/// <param name="OccurredAt">Sensör okumasının gerçekleştiği an (cihaz zamanı, UTC).</param>
/// <param name="OccurredOnUtc">Event'in üretildiği an (UTC).</param>
public sealed record TemperatureThresholdBreached(
    Guid ColdStorageUnitId,
    Guid TenantId,
    string UnitName,
    decimal TemperatureC,
    decimal MinTempC,
    decimal MaxTempC,
    bool AboveMax,
    DateTime OccurredAt,
    DateTime OccurredOnUtc) : IDomainEvent, ITenantScopedEvent;

using HalOS.BuildingBlocks.Domain;

namespace HalOS.ColdChain.Domain.Aggregates;

/// <summary>
/// Tek bir sensör okuması (docs/04 §6 zaman serisi). Soğuk hava deposunun (<see cref="ColdStorageUnit"/>)
/// parçası, APPEND-ONLY: okuma silinmez/değiştirilmez (docs/07 §8). <see cref="Id"/> cihaz/istemci
/// üretimli bir Guid'dir ve idempotency anahtarıdır — aynı okuma iki kez gelirse tekilleşir (docs/04 §5).
/// Sıcaklık/nem decimal'dir (asla float — BK-2).
/// </summary>
public sealed class SensorReading : Entity<Guid>, ITenantOwned
{
    private SensorReading(
        Guid id,
        Guid coldStorageUnitId,
        Guid tenantId,
        decimal temperatureC,
        decimal? humidityPercent,
        DateTime occurredAt)
        : base(id)
    {
        ColdStorageUnitId = coldStorageUnitId;
        TenantId = tenantId;
        TemperatureC = temperatureC;
        HumidityPercent = humidityPercent;
        OccurredAt = occurredAt;
    }

    /// <summary>ORM materialization only.</summary>
    private SensorReading()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>Ait olduğu soğuk hava deposu (aggregate kök).</summary>
    public Guid ColdStorageUnitId { get; private set; }

    /// <summary>Ölçülen sıcaklık (santigrat, NUMERIC(6,2)).</summary>
    public decimal TemperatureC { get; private set; }

    /// <summary>Ölçülen bağıl nem yüzdesi (opsiyonel; sensör desteklemiyorsa null).</summary>
    public decimal? HumidityPercent { get; private set; }

    /// <summary>Okumanın cihazda gerçekleştiği an (UTC).</summary>
    public DateTime OccurredAt { get; private set; }

    internal static SensorReading Create(
        Guid id,
        Guid coldStorageUnitId,
        Guid tenantId,
        decimal temperatureC,
        decimal? humidityPercent,
        DateTime occurredAt) =>
        new(id, coldStorageUnitId, tenantId, temperatureC, humidityPercent, occurredAt);
}

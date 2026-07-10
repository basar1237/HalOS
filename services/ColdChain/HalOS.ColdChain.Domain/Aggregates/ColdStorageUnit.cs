using HalOS.BuildingBlocks.Contracts;
using HalOS.BuildingBlocks.Domain;

namespace HalOS.ColdChain.Domain.Aggregates;

/// <summary>
/// Soğuk Hava Deposu (ColdStorageUnit) — Soğuk Zincir bağlamının kök aggregate'i (docs/04 §6,
/// docs/06 S3.1). İzlenen bir depo/oda için izin verilen sıcaklık aralığını (<see cref="MinTempC"/>..
/// <see cref="MaxTempC"/>) ve gelen sensör okumalarını (<see cref="SensorReading"/>, APPEND-ONLY zaman
/// serisi) tutar. Tenant'a bağlıdır (ITenantOwned → global query filter, BK-8).
///
/// Değişmezler:
/// - Eşik aralığı geçerli olmalıdır: <c>MinTempC &lt; MaxTempC</c>.
/// - Okumalar APPEND-ONLY; aynı <c>readingId</c> yalnız bir kez işlenir (idempotency — docs/04 §5).
/// - Bir okuma aralığın DIŞINA çıktığında (temp &gt; max ya da temp &lt; min)
///   <see cref="TemperatureThresholdBreached"/> yayınlanır (docs/04 §6: eşik aşımı → bildirim + AI
///   fire tahmini). Aralık içindeki okuma event üretmez.
/// </summary>
public sealed class ColdStorageUnit : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<SensorReading> _readings = new();

    private ColdStorageUnit(Guid id, Guid tenantId, string name, decimal minTempC, decimal maxTempC)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        MinTempC = minTempC;
        MaxTempC = maxTempC;
        IsActive = true;
    }

    /// <summary>ORM materialization only.</summary>
    private ColdStorageUnit()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>Deponun okunur adı (ör. "1 No'lu Soğuk Oda").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>İzin verilen alt sıcaklık eşiği (santigrat, NUMERIC(6,2); decimal — BK-2).</summary>
    public decimal MinTempC { get; private set; }

    /// <summary>İzin verilen üst sıcaklık eşiği (santigrat, NUMERIC(6,2); decimal — BK-2).</summary>
    public decimal MaxTempC { get; private set; }

    /// <summary>Pasifse yeni okuma kabul edilmez ve alarm üretilmez.</summary>
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<SensorReading> Readings => _readings.AsReadOnly();

    /// <summary>En son (OccurredAt en büyük) okuma; hiç okuma yoksa null. Türetilmiş.</summary>
    public SensorReading? LatestReading =>
        _readings.Count == 0 ? null : _readings.OrderByDescending(r => r.OccurredAt).First();

    /// <summary>Yeni bir soğuk hava deposu tanımlar. Ad zorunlu; alt eşik üst eşikten küçük olmalı.</summary>
    public static Result<ColdStorageUnit> Register(Guid tenantId, string name, decimal minTempC, decimal maxTempC)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ColdStorageUnit>(ColdStorageUnitErrors.NameRequired);
        }

        if (minTempC >= maxTempC)
        {
            return Result.Failure<ColdStorageUnit>(ColdStorageUnitErrors.InvalidThresholdRange);
        }

        return new ColdStorageUnit(Guid.NewGuid(), tenantId, name.Trim(), minTempC, maxTempC);
    }

    /// <summary>Sıcaklık eşiklerini günceller (alt &lt; üst olmalı). Okuma üretmez.</summary>
    public Result UpdateThresholds(decimal minTempC, decimal maxTempC)
    {
        if (minTempC >= maxTempC)
        {
            return Result.Failure(ColdStorageUnitErrors.InvalidThresholdRange);
        }

        MinTempC = minTempC;
        MaxTempC = maxTempC;
        return Result.Success();
    }

    /// <summary>Depoyu pasifleştirir (yeni okuma kabul edilmez).</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Bir sensör okumasını işler (docs/04 §6). Okuma APPEND-ONLY eklenir; sıcaklık izin verilen
    /// aralığın DIŞINDAysa <see cref="TemperatureThresholdBreached"/> yayınlanır (üst eşik aşımı →
    /// AboveMax=true, alt eşik altı → AboveMax=false). Idempotency: aynı <paramref name="readingId"/>
    /// zaten işlenmişse sessizce başarı döner, çift kayıt oluşmaz. Pasif depoda okuma reddedilir.
    /// </summary>
    public Result RecordReading(Guid readingId, decimal temperatureC, decimal? humidityPercent, DateTime occurredAt)
    {
        if (!IsActive)
        {
            return Result.Failure(ColdStorageUnitErrors.Inactive);
        }

        if (readingId == Guid.Empty)
        {
            return Result.Failure(ColdStorageUnitErrors.ReadingIdRequired);
        }

        if (humidityPercent is < 0m or > 100m)
        {
            return Result.Failure(ColdStorageUnitErrors.InvalidHumidity);
        }

        if (_readings.Any(r => r.Id == readingId))
        {
            // Cihaz/broker tekrarında sessizce yut (idempotent — docs/04 §5).
            return Result.Success();
        }

        _readings.Add(SensorReading.Create(readingId, Id, TenantId, temperatureC, humidityPercent, occurredAt));

        if (temperatureC > MaxTempC || temperatureC < MinTempC)
        {
            RaiseDomainEvent(new TemperatureThresholdBreached(
                Id,
                TenantId,
                Name,
                temperatureC,
                MinTempC,
                MaxTempC,
                AboveMax: temperatureC > MaxTempC,
                occurredAt,
                DateTime.UtcNow));
        }

        return Result.Success();
    }
}

/// <summary>Soğuk hava deposu domain hataları (docs/07 §10; kod İngilizce, mesaj Türkçe — docs/07 §3).</summary>
public static class ColdStorageUnitErrors
{
    public static readonly Error NameRequired =
        new("ColdStorageUnit.NameRequired", "Soğuk hava deposu için ad zorunludur.");

    public static readonly Error InvalidThresholdRange =
        new("ColdStorageUnit.InvalidThresholdRange", "Alt sıcaklık eşiği üst eşikten küçük olmalıdır.");

    public static readonly Error InvalidHumidity =
        new("ColdStorageUnit.InvalidHumidity", "Bağıl nem yüzdesi 0 ile 100 arasında olmalıdır.");

    public static readonly Error ReadingIdRequired =
        new("ColdStorageUnit.ReadingIdRequired", "Okuma kimliği (readingId) zorunludur.");

    public static readonly Error Inactive =
        new("ColdStorageUnit.Inactive", "Pasif soğuk hava deposuna okuma işlenemez.");

    public static readonly Error NotFound =
        new("ColdStorageUnit.NotFound", "Soğuk hava deposu bulunamadı.");
}

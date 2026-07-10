namespace HalOS.ColdChain.Api.Contracts;

/// <summary>Yeni soğuk hava deposu tanımlama isteği (docs/04 §6).</summary>
public sealed record RegisterColdStorageUnitRequest(string Name, decimal MinTempC, decimal MaxTempC);

/// <summary>Sıcaklık eşiği güncelleme isteği (docs/04 §6).</summary>
public sealed record UpdateThresholdsRequest(decimal MinTempC, decimal MaxTempC);

/// <summary>
/// Sensör okuması gönderme isteği (docs/04 §6). <see cref="ReadingId"/> cihaz üretimli idempotency
/// anahtarıdır. <see cref="OccurredAt"/> verilmezse sunucu zamanı (UtcNow) kullanılır.
/// </summary>
public sealed record RecordReadingRequest(
    Guid ReadingId,
    decimal TemperatureC,
    decimal? HumidityPercent,
    DateTime? OccurredAt);

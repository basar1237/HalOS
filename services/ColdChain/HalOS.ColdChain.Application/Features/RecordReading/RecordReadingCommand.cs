using HalOS.BuildingBlocks.Application;

namespace HalOS.ColdChain.Application.Features.RecordReading;

/// <summary>
/// Bir soğuk hava deposundan gelen sensör okumasını işler (docs/04 §6). <paramref name="ReadingId"/>
/// cihaz/istemci üretimlidir ve idempotency anahtarıdır (aynı okuma iki kez gelirse tekilleşir).
/// Sıcaklık izin verilen aralığın dışındaysa eşik-aşımı event'i yayınlanır (aggregate içinde).
/// </summary>
public sealed record RecordReadingCommand(
    Guid ColdStorageUnitId,
    Guid ReadingId,
    decimal TemperatureC,
    decimal? HumidityPercent,
    DateTime OccurredAt) : ICommand;

using HalOS.ColdChain.Domain.Aggregates;

namespace HalOS.ColdChain.Application.Contracts;

/// <summary>Tek bir sensör okuması okuma modeli (docs/04 §6 zaman serisi).</summary>
public sealed record SensorReadingDto(
    Guid Id,
    decimal TemperatureC,
    decimal? HumidityPercent,
    DateTime OccurredAt)
{
    public static SensorReadingDto FromDomain(SensorReading reading) =>
        new(reading.Id, reading.TemperatureC, reading.HumidityPercent, reading.OccurredAt);
}

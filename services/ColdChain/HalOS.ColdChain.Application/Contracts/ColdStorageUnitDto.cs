using HalOS.ColdChain.Domain.Aggregates;

namespace HalOS.ColdChain.Application.Contracts;

/// <summary>Soğuk hava deposu okuma modeli (docs/04 §6). Son okuma sıcaklığı türetilir.</summary>
public sealed record ColdStorageUnitDto(
    Guid Id,
    string Name,
    decimal MinTempC,
    decimal MaxTempC,
    bool IsActive,
    decimal? LatestTemperatureC,
    DateTime? LatestReadingAt,
    int ReadingCount)
{
    public static ColdStorageUnitDto FromDomain(ColdStorageUnit unit)
    {
        var latest = unit.LatestReading;
        return new ColdStorageUnitDto(
            unit.Id,
            unit.Name,
            unit.MinTempC,
            unit.MaxTempC,
            unit.IsActive,
            latest?.TemperatureC,
            latest?.OccurredAt,
            unit.Readings.Count);
    }
}

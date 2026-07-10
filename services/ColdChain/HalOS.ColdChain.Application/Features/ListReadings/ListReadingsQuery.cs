using HalOS.BuildingBlocks.Application;
using HalOS.ColdChain.Application.Contracts;

namespace HalOS.ColdChain.Application.Features.ListReadings;

/// <summary>Bir deponun son okumaları (OccurredAt azalan, <paramref name="Limit"/> ile sınırlı).</summary>
public sealed record ListReadingsQuery(Guid ColdStorageUnitId, int Limit = 50)
    : IQuery<IReadOnlyList<SensorReadingDto>>;

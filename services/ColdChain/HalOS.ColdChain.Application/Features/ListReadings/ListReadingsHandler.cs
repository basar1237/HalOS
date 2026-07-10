using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Application.Contracts;
using HalOS.ColdChain.Domain.Aggregates;

namespace HalOS.ColdChain.Application.Features.ListReadings;

internal sealed class ListReadingsHandler : IQueryHandler<ListReadingsQuery, IReadOnlyList<SensorReadingDto>>
{
    private readonly IColdStorageUnitRepository _units;

    public ListReadingsHandler(IColdStorageUnitRepository units)
    {
        _units = units;
    }

    public async Task<Result<IReadOnlyList<SensorReadingDto>>> Handle(
        ListReadingsQuery request,
        CancellationToken cancellationToken)
    {
        var unit = await _units.GetByIdAsync(request.ColdStorageUnitId, cancellationToken);
        if (unit is null)
        {
            return Result.Failure<IReadOnlyList<SensorReadingDto>>(ColdStorageUnitErrors.NotFound);
        }

        var limit = request.Limit is < 1 or > 500 ? 50 : request.Limit;

        IReadOnlyList<SensorReadingDto> readings = unit.Readings
            .OrderByDescending(r => r.OccurredAt)
            .Take(limit)
            .Select(SensorReadingDto.FromDomain)
            .ToList();

        return Result.Success(readings);
    }
}

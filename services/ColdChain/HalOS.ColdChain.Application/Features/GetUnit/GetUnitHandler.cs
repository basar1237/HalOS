using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Application.Contracts;
using HalOS.ColdChain.Domain.Aggregates;

namespace HalOS.ColdChain.Application.Features.GetUnit;

internal sealed class GetUnitHandler : IQueryHandler<GetUnitQuery, ColdStorageUnitDto>
{
    private readonly IColdStorageUnitRepository _units;

    public GetUnitHandler(IColdStorageUnitRepository units)
    {
        _units = units;
    }

    public async Task<Result<ColdStorageUnitDto>> Handle(GetUnitQuery request, CancellationToken cancellationToken)
    {
        var unit = await _units.GetByIdAsync(request.Id, cancellationToken);
        return unit is null
            ? Result.Failure<ColdStorageUnitDto>(ColdStorageUnitErrors.NotFound)
            : ColdStorageUnitDto.FromDomain(unit);
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Application.Contracts;

namespace HalOS.ColdChain.Application.Features.ListUnits;

internal sealed class ListUnitsHandler : IQueryHandler<ListUnitsQuery, PagedResult<ColdStorageUnitDto>>
{
    private readonly IColdStorageUnitRepository _units;

    public ListUnitsHandler(IColdStorageUnitRepository units)
    {
        _units = units;
    }

    public async Task<Result<PagedResult<ColdStorageUnitDto>>> Handle(
        ListUnitsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var result = await _units.ListAsync(page, pageSize, cancellationToken);

        return new PagedResult<ColdStorageUnitDto>(
            result.Items.Select(ColdStorageUnitDto.FromDomain).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }
}

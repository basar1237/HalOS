using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Party.Application.Abstractions;
using HalOS.Party.Application.Contracts;

namespace HalOS.Party.Application.Features.ListParties;

internal sealed class ListPartiesHandler : IQueryHandler<ListPartiesQuery, PagedResult<PartyDto>>
{
    private readonly IPartyRepository _parties;

    public ListPartiesHandler(IPartyRepository parties)
    {
        _parties = parties;
    }

    public async Task<Result<PagedResult<PartyDto>>> Handle(
        ListPartiesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _parties.ListAsync(
            request.Page,
            request.PageSize,
            request.OnlyActive,
            cancellationToken);

        var dtos = page.Items.Select(PartyDto.FromDomain).ToList();

        return new PagedResult<PartyDto>(dtos, page.Page, page.PageSize, page.TotalCount);
    }
}

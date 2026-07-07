using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Party.Application.Abstractions;
using HalOS.Party.Application.Contracts;
using HalOS.Party.Domain.Aggregates;

namespace HalOS.Party.Application.Features.GetParty;

internal sealed class GetPartyHandler : IQueryHandler<GetPartyQuery, PartyDto>
{
    private readonly IPartyRepository _parties;

    public GetPartyHandler(IPartyRepository parties)
    {
        _parties = parties;
    }

    public async Task<Result<PartyDto>> Handle(GetPartyQuery request, CancellationToken cancellationToken)
    {
        var party = await _parties.GetByIdAsync(request.PartyId, cancellationToken);
        if (party is null)
        {
            return Result.Failure<PartyDto>(PartyErrors.NotFound);
        }

        return PartyDto.FromDomain(party);
    }
}

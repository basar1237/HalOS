using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Party.Application.Abstractions;
using HalOS.Party.Domain.Aggregates;

namespace HalOS.Party.Application.Features.AddPartyRole;

internal sealed class AddPartyRoleHandler : ICommandHandler<AddPartyRoleCommand>
{
    private readonly IPartyRepository _parties;
    private readonly IUnitOfWork _unitOfWork;

    public AddPartyRoleHandler(IPartyRepository parties, IUnitOfWork unitOfWork)
    {
        _parties = parties;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddPartyRoleCommand request, CancellationToken cancellationToken)
    {
        var party = await _parties.GetByIdAsync(request.PartyId, cancellationToken);
        if (party is null)
        {
            return Result.Failure(PartyErrors.NotFound);
        }

        var result = party.AddRole(request.Type);
        if (result.IsFailure)
        {
            return result;
        }

        _parties.Update(party);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Party.Application.Abstractions;
using HalOS.Party.Domain.Aggregates;
using HalOS.Party.Domain.ValueObjects;

namespace HalOS.Party.Application.Features.UpdateParty;

internal sealed class UpdatePartyHandler : ICommandHandler<UpdatePartyCommand>
{
    private readonly IPartyRepository _parties;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePartyHandler(IPartyRepository parties, IUnitOfWork unitOfWork)
    {
        _parties = parties;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdatePartyCommand request, CancellationToken cancellationToken)
    {
        // GetById tenant global query filter'a tabidir → çapraz-tenant güncelleme engellenir (BK-8).
        var party = await _parties.GetByIdAsync(request.PartyId, cancellationToken);
        if (party is null)
        {
            return Result.Failure(PartyErrors.NotFound);
        }

        WithholdingProfile? profile = null;
        if (request.WithholdingProfile is not null)
        {
            var profileResult = WithholdingProfile.Create(
                request.WithholdingProfile.AgriWithholdingRate,
                request.WithholdingProfile.FarmerSskRate);
            if (profileResult.IsFailure)
            {
                return Result.Failure(profileResult.Error);
            }

            profile = profileResult.Value;
        }

        var updateResult = party.Update(
            request.DisplayName,
            request.TaxOffice,
            request.Phone,
            request.Address,
            request.KeepsRecords,
            profile);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        _parties.Update(party);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

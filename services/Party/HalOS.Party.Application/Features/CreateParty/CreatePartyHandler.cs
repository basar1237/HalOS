using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Party.Application.Abstractions;
using HalOS.Party.Domain.Aggregates;
using HalOS.Party.Domain.ValueObjects;
using PartyAggregate = HalOS.Party.Domain.Aggregates.Party;

namespace HalOS.Party.Application.Features.CreateParty;

internal sealed class CreatePartyHandler : ICommandHandler<CreatePartyCommand, Guid>
{
    private readonly IPartyRepository _parties;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePartyHandler(
        IPartyRepository parties,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _parties = parties;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreatePartyCommand request,
        CancellationToken cancellationToken)
    {
        var tckn = string.IsNullOrWhiteSpace(request.Tckn) ? null : request.Tckn.Trim();
        var vkn = string.IsNullOrWhiteSpace(request.Vkn) ? null : request.Vkn.Trim();

        // Tekillik ön-kontrolü tenant içinde (docs/02 §3.1); DB unique index nihai garantidir.
        if (tckn is not null && await _parties.ExistsByTcknAsync(tckn, cancellationToken))
        {
            return Result.Failure<Guid>(PartyErrors.TcknAlreadyInUse);
        }

        if (vkn is not null && await _parties.ExistsByVknAsync(vkn, cancellationToken))
        {
            return Result.Failure<Guid>(PartyErrors.VknAlreadyInUse);
        }

        WithholdingProfile? profile = null;
        if (request.WithholdingProfile is not null)
        {
            var profileResult = WithholdingProfile.Create(
                request.WithholdingProfile.AgriWithholdingRate,
                request.WithholdingProfile.FarmerSskRate);
            if (profileResult.IsFailure)
            {
                return Result.Failure<Guid>(profileResult.Error);
            }

            profile = profileResult.Value;
        }

        var partyResult = PartyAggregate.Register(
            _tenantContext.TenantId,
            request.DisplayName,
            tckn,
            vkn,
            request.TaxOffice,
            request.Phone,
            request.Address,
            request.KeepsRecords,
            profile,
            request.Roles);

        if (partyResult.IsFailure)
        {
            return Result.Failure<Guid>(partyResult.Error);
        }

        var party = partyResult.Value;
        _parties.Add(party);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return party.Id;
    }
}

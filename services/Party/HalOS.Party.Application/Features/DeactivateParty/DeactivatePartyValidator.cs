using FluentValidation;

namespace HalOS.Party.Application.Features.DeactivateParty;

public sealed class DeactivatePartyValidator : AbstractValidator<DeactivatePartyCommand>
{
    public DeactivatePartyValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty().WithMessage("Taraf kimliği zorunludur.");
    }
}

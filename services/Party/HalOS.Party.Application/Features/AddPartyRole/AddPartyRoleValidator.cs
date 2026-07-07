using FluentValidation;

namespace HalOS.Party.Application.Features.AddPartyRole;

public sealed class AddPartyRoleValidator : AbstractValidator<AddPartyRoleCommand>
{
    public AddPartyRoleValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty().WithMessage("Taraf kimliği zorunludur.");
        RuleFor(x => x.Type).IsInEnum().WithMessage("Geçersiz rol tipi.");
    }
}

using FluentValidation;

namespace HalOS.Party.Application.Features.UpdateParty;

public sealed class UpdatePartyValidator : AbstractValidator<UpdatePartyCommand>
{
    public UpdatePartyValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty().WithMessage("Taraf kimliği zorunludur.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Taraf adı zorunludur.")
            .MaximumLength(200).WithMessage("Taraf adı çok uzun.");

        When(x => x.WithholdingProfile is not null, () =>
        {
            RuleFor(x => x.WithholdingProfile!.AgriWithholdingRate)
                .InclusiveBetween(0m, 1m).WithMessage("Zirai stopaj oranı 0 ile 1 arasında olmalıdır.");
            RuleFor(x => x.WithholdingProfile!.FarmerSskRate)
                .InclusiveBetween(0m, 1m).WithMessage("Çiftçi Bağ-Kur oranı 0 ile 1 arasında olmalıdır.");
        });
    }
}

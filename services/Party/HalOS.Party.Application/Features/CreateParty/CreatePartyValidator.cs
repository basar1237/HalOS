using FluentValidation;
using PartyAggregate = HalOS.Party.Domain.Aggregates.Party;

namespace HalOS.Party.Application.Features.CreateParty;

/// <summary>
/// CreateParty girdi doğrulaması (docs/07 §5). TCKN 11 hane / VKN 10 hane format; en az bir rol.
/// İş kuralı (Producer → stopaj profili) domain'de korunur; burada yalnızca yüzeysel format.
/// </summary>
public sealed class CreatePartyValidator : AbstractValidator<CreatePartyCommand>
{
    public CreatePartyValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Taraf adı zorunludur.")
            .MaximumLength(200).WithMessage("Taraf adı çok uzun.");

        // TCKN doluysa 11 haneli rakam olmalı.
        When(x => !string.IsNullOrWhiteSpace(x.Tckn), () =>
        {
            RuleFor(x => x.Tckn!)
                .Must(v => v.Length == PartyAggregate.TcknLength && v.All(char.IsDigit))
                .WithMessage("TCKN 11 haneli rakamlardan oluşmalıdır.");
        });

        // VKN doluysa 10 haneli rakam olmalı.
        When(x => !string.IsNullOrWhiteSpace(x.Vkn), () =>
        {
            RuleFor(x => x.Vkn!)
                .Must(v => v.Length == PartyAggregate.VknLength && v.All(char.IsDigit))
                .WithMessage("VKN 10 haneli rakamlardan oluşmalıdır.");
        });

        RuleFor(x => x.Roles)
            .NotNull().WithMessage("En az bir rol tanımlanmalıdır.")
            .Must(roles => roles is { Count: > 0 }).WithMessage("En az bir rol tanımlanmalıdır.");

        When(x => x.WithholdingProfile is not null, () =>
        {
            RuleFor(x => x.WithholdingProfile!.AgriWithholdingRate)
                .InclusiveBetween(0m, 1m).WithMessage("Zirai stopaj oranı 0 ile 1 arasında olmalıdır.");
            RuleFor(x => x.WithholdingProfile!.FarmerSskRate)
                .InclusiveBetween(0m, 1m).WithMessage("Çiftçi Bağ-Kur oranı 0 ile 1 arasında olmalıdır.");
        });
    }
}

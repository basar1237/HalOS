using FluentValidation;

namespace HalOS.ColdChain.Application.Features.RegisterColdStorageUnit;

/// <summary>RegisterColdStorageUnit girdi doğrulaması (docs/07 §5). Ad zorunlu; alt eşik &lt; üst eşik.</summary>
public sealed class RegisterColdStorageUnitValidator : AbstractValidator<RegisterColdStorageUnitCommand>
{
    public RegisterColdStorageUnitValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Soğuk hava deposu için ad zorunludur.");
        RuleFor(x => x.MinTempC)
            .LessThan(x => x.MaxTempC)
            .WithMessage("Alt sıcaklık eşiği üst eşikten küçük olmalıdır.");
    }
}

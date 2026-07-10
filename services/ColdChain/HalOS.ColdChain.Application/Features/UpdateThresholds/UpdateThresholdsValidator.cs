using FluentValidation;

namespace HalOS.ColdChain.Application.Features.UpdateThresholds;

/// <summary>UpdateThresholds girdi doğrulaması (docs/07 §5). Depo zorunlu; alt eşik &lt; üst eşik.</summary>
public sealed class UpdateThresholdsValidator : AbstractValidator<UpdateThresholdsCommand>
{
    public UpdateThresholdsValidator()
    {
        RuleFor(x => x.ColdStorageUnitId).NotEmpty().WithMessage("Depo referansı zorunludur.");
        RuleFor(x => x.MinTempC)
            .LessThan(x => x.MaxTempC)
            .WithMessage("Alt sıcaklık eşiği üst eşikten küçük olmalıdır.");
    }
}

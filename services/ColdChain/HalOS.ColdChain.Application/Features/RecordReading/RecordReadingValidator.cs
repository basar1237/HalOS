using FluentValidation;

namespace HalOS.ColdChain.Application.Features.RecordReading;

/// <summary>RecordReading girdi doğrulaması (docs/07 §5). Depo ve okuma kimliği zorunlu.</summary>
public sealed class RecordReadingValidator : AbstractValidator<RecordReadingCommand>
{
    public RecordReadingValidator()
    {
        RuleFor(x => x.ColdStorageUnitId).NotEmpty().WithMessage("Depo referansı zorunludur.");
        RuleFor(x => x.ReadingId).NotEmpty().WithMessage("Okuma kimliği (readingId) zorunludur.");
    }
}

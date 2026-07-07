using FluentValidation;
using HalOS.Finance.Domain.Aggregates;
using HalOS.Finance.Domain.Enums;

namespace HalOS.Finance.Application.Features.RecordAdvance;

/// <summary>
/// RecordAdvance girdi doğrulaması (docs/07 §5). Taraf zorunlu, tutar pozitif. BK-6 nakit eşiği
/// (7.000 TL) erken/net uyarı olarak da doğrulanır; nihai koruma domain
/// <see cref="CurrentAccount.RecordAdvance"/> içindedir (çift savunma).
/// </summary>
public sealed class RecordAdvanceValidator : AbstractValidator<RecordAdvanceCommand>
{
    public RecordAdvanceValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty().WithMessage("Taraf (party) referansı zorunludur.");
        RuleFor(x => x.Amount).GreaterThan(0m).WithMessage("Avans tutarı sıfırdan büyük olmalıdır.");

        RuleFor(x => x.Amount)
            .LessThanOrEqualTo(CurrentAccount.CashLimit)
            .When(x => x.Channel == PaymentChannel.Cash)
            .WithMessage("7.000 TL üstü avans nakit verilemez; banka üzerinden ve belgeli olmalıdır (BK-6).");
    }
}

using FluentValidation;
using HalOS.Finance.Domain.Aggregates;
using HalOS.Finance.Domain.Enums;

namespace HalOS.Finance.Application.Features.RecordCollection;

/// <summary>
/// RecordCollection girdi doğrulaması (docs/07 §5). Taraf zorunlu, tutar pozitif. BK-6 nakit eşiği
/// (7.000 TL) erken/net uyarı olarak da doğrulanır; nihai koruma domain
/// <see cref="CurrentAccount.RecordCollection"/> içindedir (çift savunma).
/// </summary>
public sealed class RecordCollectionValidator : AbstractValidator<RecordCollectionCommand>
{
    public RecordCollectionValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty().WithMessage("Alıcı (taraf) referansı zorunludur.");
        RuleFor(x => x.Amount).GreaterThan(0m).WithMessage("Tahsilat tutarı sıfırdan büyük olmalıdır.");

        RuleFor(x => x.Amount)
            .LessThanOrEqualTo(CurrentAccount.CashLimit)
            .When(x => x.Channel == PaymentChannel.Cash)
            .WithMessage("7.000 TL üstü tahsilat nakit yapılamaz; banka üzerinden ve belgeli olmalıdır (BK-6).");
    }
}

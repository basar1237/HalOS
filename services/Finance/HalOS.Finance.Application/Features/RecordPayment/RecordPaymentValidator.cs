using FluentValidation;
using HalOS.Finance.Domain.Aggregates;
using HalOS.Finance.Domain.Enums;

namespace HalOS.Finance.Application.Features.RecordPayment;

/// <summary>
/// RecordPayment girdi doğrulaması (docs/07 §5). Taraf zorunlu, tutar pozitif. BK-6 nakit eşiği
/// (7.000 TL) burada kullanıcıya erken/net uyarı olarak da doğrulanır; nihai değişmez koruması
/// domain <see cref="CurrentAccount.RecordPayment"/> içindedir (çift savunma).
/// </summary>
public sealed class RecordPaymentValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty().WithMessage("Müstahsil (taraf) referansı zorunludur.");
        RuleFor(x => x.Amount).GreaterThan(0m).WithMessage("Ödeme tutarı sıfırdan büyük olmalıdır.");

        // BK-6: 7.000 TL üstü nakit yasak; banka üzerinden ve belgeli olmalı.
        RuleFor(x => x.Amount)
            .LessThanOrEqualTo(CurrentAccount.CashLimit)
            .When(x => x.Channel == PaymentChannel.Cash)
            .WithMessage("7.000 TL üstü ödeme nakit yapılamaz; banka üzerinden ve belgeli olmalıdır (BK-6).");
    }
}

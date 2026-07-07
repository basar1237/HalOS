using FluentValidation;

namespace HalOS.Sales.Application.Features.ReceiveConsignment;

/// <summary>
/// ReceiveConsignment girdi doğrulaması (docs/07 §5). Müstahsil zorunlu; en az bir kalem;
/// miktar &gt; 0. İş kuralı domain'de de korunur (Consignment.Receive).
/// </summary>
public sealed class ReceiveConsignmentValidator : AbstractValidator<ReceiveConsignmentCommand>
{
    public ReceiveConsignmentValidator()
    {
        RuleFor(x => x.ProducerPartyId)
            .NotEmpty().WithMessage("Müstahsil (üretici) referansı zorunludur.");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Mal geliş en az bir kalem içermelidir.")
            .Must(items => items is { Count: > 0 }).WithMessage("Mal geliş en az bir kalem içermelidir.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("Kalem için ürün referansı zorunludur.");
            item.RuleFor(i => i.Quantity).GreaterThan(0m).WithMessage("Kalem miktarı sıfırdan büyük olmalıdır.");
        });
    }
}

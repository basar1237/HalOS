using FluentValidation;

namespace HalOS.Integration.Application.Features.ReissueProducerReceipt;

/// <summary>ReissueProducerReceipt doğrulaması (docs/07 §5).</summary>
public sealed class ReissueProducerReceiptValidator : AbstractValidator<ReissueProducerReceiptCommand>
{
    public ReissueProducerReceiptValidator()
    {
        RuleFor(x => x.ReceiptId).NotEmpty().WithMessage("Makbuz kimliği zorunludur.");
    }
}

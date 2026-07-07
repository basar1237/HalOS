using FluentValidation;

namespace HalOS.Integration.Application.Features.ReissueInvoice;

/// <summary>ReissueInvoice doğrulaması (docs/07 §5).</summary>
public sealed class ReissueInvoiceValidator : AbstractValidator<ReissueInvoiceCommand>
{
    public ReissueInvoiceValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithMessage("Fatura kimliği zorunludur.");
    }
}

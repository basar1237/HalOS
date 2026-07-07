using FluentValidation;

namespace HalOS.Sales.Application.Features.CompleteSale;

/// <summary>CompleteSale girdi doğrulaması (docs/07 §5).</summary>
public sealed class CompleteSaleValidator : AbstractValidator<CompleteSaleCommand>
{
    public CompleteSaleValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty().WithMessage("Satış referansı zorunludur.");
    }
}

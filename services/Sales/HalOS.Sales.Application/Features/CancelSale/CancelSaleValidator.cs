using FluentValidation;

namespace HalOS.Sales.Application.Features.CancelSale;

/// <summary>CancelSale girdi doğrulaması (docs/07 §5). İptal gerekçesi denetim için önerilir.</summary>
public sealed class CancelSaleValidator : AbstractValidator<CancelSaleCommand>
{
    public CancelSaleValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty().WithMessage("Satış referansı zorunludur.");
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("İptal gerekçesi çok uzun.");
    }
}

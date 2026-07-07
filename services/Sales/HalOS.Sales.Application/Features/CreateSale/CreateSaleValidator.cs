using FluentValidation;

namespace HalOS.Sales.Application.Features.CreateSale;

/// <summary>CreateSale girdi doğrulaması (docs/07 §5). Alıcı ve müstahsil zorunlu.</summary>
public sealed class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.BuyerPartyId).NotEmpty().WithMessage("Alıcı referansı zorunludur.");
        RuleFor(x => x.ProducerPartyId).NotEmpty().WithMessage("Müstahsil referansı zorunludur.");
    }
}

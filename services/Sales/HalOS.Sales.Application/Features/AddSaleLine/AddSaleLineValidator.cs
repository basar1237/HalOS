using FluentValidation;

namespace HalOS.Sales.Application.Features.AddSaleLine;

/// <summary>
/// AddSaleLine girdi doğrulaması (docs/07 §5): satır&gt;0 (ürün var), miktar&gt;0, fiyat≥0.
/// Aynı kurallar domain'de de (SaleTransaction.AddLine) korunur.
/// </summary>
public sealed class AddSaleLineValidator : AbstractValidator<AddSaleLineCommand>
{
    public AddSaleLineValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty().WithMessage("Satış referansı zorunludur.");
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Ürün referansı zorunludur.");
        RuleFor(x => x.Quantity).GreaterThan(0m).WithMessage("Satır miktarı sıfırdan büyük olmalıdır.");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m).WithMessage("Birim fiyat negatif olamaz.");
    }
}

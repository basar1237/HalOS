using FluentValidation;

namespace HalOS.Inventory.Application.Features.CreateProduct;

/// <summary>
/// CreateProduct girdi doğrulaması (docs/07 §5). Ad zorunlu (nihai değişmez domain'de de korunur —
/// çift savunma); kategori opsiyonel uzunluk sınırı.
/// </summary>
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Kategori en fazla 100 karakter olabilir.");
    }
}

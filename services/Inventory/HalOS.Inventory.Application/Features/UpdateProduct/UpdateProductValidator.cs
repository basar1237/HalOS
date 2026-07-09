using FluentValidation;

namespace HalOS.Inventory.Application.Features.UpdateProduct;

/// <summary>UpdateProduct girdi doğrulaması (docs/07 §5). CreateProductValidator ile aynı kurallar.</summary>
public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Kategori en fazla 100 karakter olabilir.");
    }
}

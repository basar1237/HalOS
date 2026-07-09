using FluentValidation;

namespace HalOS.Inventory.Application.Features.ListProducts;

/// <summary>ListProducts sayfalama doğrulaması (docs/07 §5). ListStockValidator deseniyle birebir.</summary>
public sealed class ListProductsValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa 1'den küçük olamaz.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .WithMessage("Sayfa boyutu 1 ile 200 arasında olmalıdır.");
    }
}

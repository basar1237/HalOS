using FluentValidation;

namespace HalOS.Sales.Application.Features.ListSales;

/// <summary>ListSales sayfalama sınırları doğrulaması (docs/07 §5).</summary>
public sealed class ListSalesValidator : AbstractValidator<ListSalesQuery>
{
    public ListSalesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("Sayfa boyutu 1 ile 200 arasında olmalıdır.");
    }
}

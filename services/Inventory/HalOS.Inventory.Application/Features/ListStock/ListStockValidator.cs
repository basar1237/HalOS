using FluentValidation;

namespace HalOS.Inventory.Application.Features.ListStock;

/// <summary>ListStock sayfalama doğrulaması (docs/07 §5). Finance.ListCurrentAccountsValidator deseniyle birebir.</summary>
public sealed class ListStockValidator : AbstractValidator<ListStockQuery>
{
    public ListStockValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa boyutu 1 veya daha büyük olmalıdır.")
            .LessThanOrEqualTo(200).WithMessage("Sayfa boyutu en fazla 200 olabilir.");
    }
}

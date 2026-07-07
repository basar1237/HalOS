using FluentValidation;

namespace HalOS.Finance.Application.Features.ListCurrentAccounts;

/// <summary>ListCurrentAccounts sayfalama sınırları doğrulaması (docs/07 §5). Sales deseniyle birebir.</summary>
public sealed class ListCurrentAccountsValidator : AbstractValidator<ListCurrentAccountsQuery>
{
    public ListCurrentAccountsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("Sayfa boyutu 1 ile 200 arasında olmalıdır.");
    }
}

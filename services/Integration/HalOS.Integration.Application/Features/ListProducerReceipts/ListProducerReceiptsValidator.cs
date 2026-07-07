using FluentValidation;

namespace HalOS.Integration.Application.Features.ListProducerReceipts;

/// <summary>ListProducerReceipts sayfalama sınırları doğrulaması (docs/07 §5). Finance deseniyle birebir.</summary>
public sealed class ListProducerReceiptsValidator : AbstractValidator<ListProducerReceiptsQuery>
{
    public ListProducerReceiptsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("Sayfa boyutu 1 ile 200 arasında olmalıdır.");
    }
}

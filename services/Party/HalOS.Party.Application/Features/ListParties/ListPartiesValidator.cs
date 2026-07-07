using FluentValidation;

namespace HalOS.Party.Application.Features.ListParties;

public sealed class ListPartiesValidator : AbstractValidator<ListPartiesQuery>
{
    /// <summary>Sayfa boyutu üst sınırı (aşırı büyük sayfa isteklerini engeller).</summary>
    public const int MaxPageSize = 200;

    public ListPartiesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"Sayfa boyutu 1 ile {MaxPageSize} arasında olmalıdır.");
    }
}

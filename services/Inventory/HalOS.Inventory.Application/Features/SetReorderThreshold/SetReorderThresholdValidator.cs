using FluentValidation;

namespace HalOS.Inventory.Application.Features.SetReorderThreshold;

/// <summary>
/// SetReorderThreshold girdi doğrulaması (docs/07 §5). Ürün zorunlu; eşik verildiyse negatif olamaz
/// (null geçerli — uyarıyı kaldırır). Nihai negatif koruması domain
/// <see cref="HalOS.Inventory.Domain.Aggregates.StockItem.SetReorderThreshold"/> içindedir (çift savunma).
/// </summary>
public sealed class SetReorderThresholdValidator : AbstractValidator<SetReorderThresholdCommand>
{
    public SetReorderThresholdValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Ürün referansı zorunludur.");
        RuleFor(x => x.ReorderThreshold!.Value)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.ReorderThreshold.HasValue)
            .WithMessage("Yeniden-sipariş eşiği negatif olamaz.");
    }
}

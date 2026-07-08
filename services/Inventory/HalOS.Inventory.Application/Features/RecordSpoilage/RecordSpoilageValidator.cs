using FluentValidation;

namespace HalOS.Inventory.Application.Features.RecordSpoilage;

/// <summary>
/// RecordSpoilage girdi doğrulaması (docs/07 §5). Ürün zorunlu, miktar pozitif, gerekçe zorunlu.
/// Nihai değişmez koruması (miktar &gt; 0, gerekçe, BK-7 stok aşımı) domain
/// <see cref="HalOS.Inventory.Domain.Aggregates.StockItem.RecordSpoilage"/> içindedir (çift savunma).
/// </summary>
public sealed class RecordSpoilageValidator : AbstractValidator<RecordSpoilageCommand>
{
    public RecordSpoilageValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Ürün referansı zorunludur.");
        RuleFor(x => x.Quantity).GreaterThan(0m).WithMessage("Fire miktarı sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Fire kaydı için gerekçe zorunludur.");
    }
}

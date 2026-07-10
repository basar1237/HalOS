using FluentValidation;

namespace HalOS.Sales.Application.Features.SyncOfflineSale;

/// <summary>SyncOfflineSale girdi doğrulaması (docs/07 §5). Alıcı, müstahsil ve en az bir satır zorunlu.</summary>
public sealed class SyncOfflineSaleValidator : AbstractValidator<SyncOfflineSaleCommand>
{
    public SyncOfflineSaleValidator()
    {
        RuleFor(x => x.BuyerPartyId).NotEmpty().WithMessage("Alıcı referansı zorunludur.");
        RuleFor(x => x.ProducerPartyId).NotEmpty().WithMessage("Müstahsil referansı zorunludur.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Satışta en az bir satır olmalıdır.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty().WithMessage("Satır için ürün referansı zorunludur.");
            line.RuleFor(l => l.Quantity).GreaterThan(0m).WithMessage("Satır miktarı sıfırdan büyük olmalıdır.");
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0m).WithMessage("Birim fiyat negatif olamaz.");
        });
    }
}

using FluentValidation;

namespace HalOS.Inventory.Application.Features.CreateWarehouse;

/// <summary>
/// CreateWarehouse girdi doğrulaması (docs/07 §5). Ad ve kod zorunlu. Nihai değişmezler (ad/kod
/// boş olamaz) domain <see cref="HalOS.Inventory.Domain.Aggregates.Warehouse.Create"/> içinde de
/// korunur (çift savunma); kod tekilliği handler + UNIQUE(tenant_id, code) ile korunur.
/// </summary>
public sealed class CreateWarehouseValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Depo adı zorunludur.");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Depo kodu zorunludur.")
            .MaximumLength(32).WithMessage("Depo kodu en fazla 32 karakter olabilir.");
    }
}

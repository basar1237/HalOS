using FluentValidation;

namespace HalOS.Identity.Application.Features.Tenants.CreateTenant;

public sealed class CreateTenantValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("İşletme adı zorunludur.")
            .MaximumLength(200).WithMessage("İşletme adı çok uzun.");
    }
}

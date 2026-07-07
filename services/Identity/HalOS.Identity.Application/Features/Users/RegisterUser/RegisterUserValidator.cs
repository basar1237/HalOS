using FluentValidation;

namespace HalOS.Identity.Application.Features.Users.RegisterUser;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant belirtilmelidir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("E-posta geçersiz.")
            .MaximumLength(320);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola zorunludur.")
            .MinimumLength(8).WithMessage("Parola en az 8 karakter olmalıdır.")
            .MaximumLength(128);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(200);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Rol geçersiz.");
    }
}

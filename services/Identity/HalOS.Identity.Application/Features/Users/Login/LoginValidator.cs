using FluentValidation;

namespace HalOS.Identity.Application.Features.Users.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("E-posta geçersiz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola zorunludur.");
    }
}

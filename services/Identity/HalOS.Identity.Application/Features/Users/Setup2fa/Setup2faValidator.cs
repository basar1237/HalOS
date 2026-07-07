using FluentValidation;

namespace HalOS.Identity.Application.Features.Users.Setup2fa;

/// <summary>
/// Bu komutun girdi alanı yoktur; kimlik doğrulama handler'da kontrol edilir. Validator,
/// FluentValidation pipeline'ının her istek için tutarlı çalışması adına yine de tanımlıdır.
/// </summary>
public sealed class Setup2faValidator : AbstractValidator<Setup2faCommand>
{
    public Setup2faValidator()
    {
    }
}

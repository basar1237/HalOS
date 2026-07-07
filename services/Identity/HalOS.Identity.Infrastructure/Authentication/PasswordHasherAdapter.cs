using HalOS.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace HalOS.Identity.Infrastructure.Authentication;

/// <summary>
/// ASP.NET Core Identity <see cref="PasswordHasher{TUser}"/> tabanlı parola hash'leme
/// (docs/07 §güvenlik). Domain'e bağımlı değildir; jenerik bir marker tip kullanılır.
/// </summary>
internal sealed class PasswordHasherAdapter : IPasswordHasher
{
    private sealed class PasswordOwner;

    private readonly PasswordHasher<PasswordOwner> _hasher = new();
    private static readonly PasswordOwner Owner = new();

    public string Hash(string password) => _hasher.HashPassword(Owner, password);

    public bool Verify(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(Owner, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}

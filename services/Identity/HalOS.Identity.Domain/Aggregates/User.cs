using HalOS.BuildingBlocks.Domain;
using HalOS.Identity.Domain.Enums;
using HalOS.Identity.Domain.Events;
using HalOS.Identity.Domain.ValueObjects;

namespace HalOS.Identity.Domain.Aggregates;

/// <summary>
/// Kullanıcı aggregate'i (docs/02 §1 <c>User</c>, docs/04 ADR-009). Kimlik doğrulama,
/// rol, 2FA (TOTP) ve refresh token yaşam döngüsünü yönetir. Tenant'a bağlıdır
/// (ITenantOwned → global query filter, docs/07 §6).
/// </summary>
public sealed class User : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<RefreshToken> _refreshTokens = new();

    private User(
        Guid id,
        Guid tenantId,
        Email email,
        PasswordHash passwordHash,
        string fullName,
        SystemRole role,
        DateTime createdOnUtc)
        : base(id)
    {
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        Role = role;
        IsActive = true;
        CreatedOnUtc = createdOnUtc;
    }

    private User()
    {
        FullName = string.Empty;
        Email = null!;
        PasswordHash = null!;
    }

    public Guid TenantId { get; private set; }

    public Email Email { get; private set; }

    public PasswordHash PasswordHash { get; private set; }

    public string FullName { get; private set; }

    public SystemRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    /// <summary>2FA doğrulandıysa true. Kurulum başlatılsa da doğrulanana dek false kalır.</summary>
    public bool TwoFactorEnabled { get; private set; }

    /// <summary>TOTP paylaşılan gizli anahtarı (base32). 2FA kurulumu ile atanır.</summary>
    public string? TwoFactorSecret { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static Result<User> Register(
        Guid tenantId,
        Email email,
        PasswordHash passwordHash,
        string? fullName,
        SystemRole role)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure<User>(UserErrors.FullNameRequired);
        }

        var user = new User(
            Guid.NewGuid(),
            tenantId,
            email,
            passwordHash,
            fullName.Trim(),
            role,
            DateTime.UtcNow);

        user.RaiseDomainEvent(
            new UserRegistered(user.Id, tenantId, email.Value, user.CreatedOnUtc));

        return user;
    }

    /// <summary>2FA kurulumunu başlatır: gizli anahtar atanır ancak doğrulanana dek etkin olmaz.</summary>
    public Result BeginTwoFactorSetup(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Result.Failure(UserErrors.TwoFactorSecretRequired);
        }

        if (TwoFactorEnabled)
        {
            return Result.Failure(UserErrors.TwoFactorAlreadyEnabled);
        }

        TwoFactorSecret = secret;
        return Result.Success();
    }

    /// <summary>TOTP kodu doğrulandıktan sonra 2FA'yı etkinleştirir.</summary>
    public Result EnableTwoFactor()
    {
        if (string.IsNullOrWhiteSpace(TwoFactorSecret))
        {
            return Result.Failure(UserErrors.TwoFactorNotSetUp);
        }

        TwoFactorEnabled = true;
        RaiseDomainEvent(new UserTwoFactorEnabled(Id, TenantId, DateTime.UtcNow));
        return Result.Success();
    }

    public RefreshToken IssueRefreshToken(string tokenHash, DateTime expiresOnUtc)
    {
        var token = RefreshToken.Issue(Id, tokenHash, expiresOnUtc);
        _refreshTokens.Add(token);
        return token;
    }

    /// <summary>Verilen hash'e karşılık gelen aktif refresh token'ı iptal eder (rotasyon).</summary>
    public Result RevokeRefreshToken(string tokenHash)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        if (token is null || !token.IsActive)
        {
            return Result.Failure(UserErrors.RefreshTokenInvalid);
        }

        token.Revoke();
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;
}

public static class UserErrors
{
    public static readonly Error FullNameRequired =
        new("User.FullNameRequired", "Ad soyad zorunludur.");

    public static readonly Error EmailAlreadyInUse =
        new("User.EmailAlreadyInUse", "Bu e-posta adresi zaten kullanımda.");

    public static readonly Error InvalidCredentials =
        new("User.InvalidCredentials", "E-posta veya parola hatalı.");

    public static readonly Error Inactive =
        new("User.Inactive", "Kullanıcı hesabı aktif değil.");

    public static readonly Error TwoFactorSecretRequired =
        new("User.TwoFactorSecretRequired", "2FA gizli anahtarı zorunludur.");

    public static readonly Error TwoFactorAlreadyEnabled =
        new("User.TwoFactorAlreadyEnabled", "2FA zaten etkin.");

    public static readonly Error TwoFactorNotSetUp =
        new("User.TwoFactorNotSetUp", "2FA kurulumu başlatılmamış.");

    public static readonly Error TwoFactorInvalidCode =
        new("User.TwoFactorInvalidCode", "2FA kodu geçersiz.");

    public static readonly Error TwoFactorRequired =
        new("User.TwoFactorRequired", "2FA kodu gerekli.");

    public static readonly Error RefreshTokenInvalid =
        new("User.RefreshTokenInvalid", "Refresh token geçersiz veya süresi dolmuş.");

    public static readonly Error NotFound =
        new("User.NotFound", "Kullanıcı bulunamadı.");
}

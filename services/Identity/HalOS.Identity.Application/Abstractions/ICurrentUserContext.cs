namespace HalOS.Identity.Application.Abstractions;

/// <summary>
/// O anki isteğin kimliği doğrulanmış kullanıcısını sağlar (JWT "sub" claim'inden).
/// ITenantContext tenant'ı taşırken bu port kullanıcıyı taşır (docs/04 §7).
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Kimliği doğrulanmış kullanıcının Id'si; anonim istekte null.</summary>
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}

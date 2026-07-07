namespace HalOS.Finance.Application.Abstractions;

/// <summary>
/// Geçerli isteğin kullanıcısını sağlar (JWT subject claim'inden). Mali kayıtların denetim
/// alanları için kullanılır (docs/05 §1 denetim, docs/03 §6 audit). Anonim/sistem bağlamlarında
/// <see cref="Guid.Empty"/> döner (Sales deseniyle birebir).
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Geçerli kullanıcı kimliği; yoksa <see cref="Guid.Empty"/>.</summary>
    Guid UserId { get; }
}

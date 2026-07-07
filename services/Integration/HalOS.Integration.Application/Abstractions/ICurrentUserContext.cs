namespace HalOS.Integration.Application.Abstractions;

/// <summary>
/// Geçerli isteğin kullanıcısını sağlar (JWT subject claim'inden). Yasal belge işlemlerinin
/// denetim alanları için kullanılır (docs/05 §1 denetim, docs/03 §6 audit). Anonim/sistem
/// bağlamlarında <see cref="Guid.Empty"/> döner (Finance/Sales deseniyle birebir).
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Geçerli kullanıcı kimliği; yoksa <see cref="Guid.Empty"/>.</summary>
    Guid UserId { get; }
}

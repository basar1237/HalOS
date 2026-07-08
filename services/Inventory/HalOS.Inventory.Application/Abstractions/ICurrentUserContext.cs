namespace HalOS.Inventory.Application.Abstractions;

/// <summary>
/// Geçerli isteğin kullanıcısını sağlar (JWT subject claim'inden). Stok/fire kayıtlarının denetim
/// alanları için kullanılır (docs/05 §1 denetim, docs/03 §6 audit). Anonim/sistem bağlamlarında
/// <see cref="Guid.Empty"/> döner (Finance deseniyle birebir).
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Geçerli kullanıcı kimliği; yoksa <see cref="Guid.Empty"/>.</summary>
    Guid UserId { get; }
}

using HalOS.BuildingBlocks.Application;
using HalOS.Identity.Application.Abstractions;

namespace HalOS.Identity.Api.Authentication;

/// <summary>
/// Identity'nin mevcut <see cref="ICurrentUserContext"/>'ini (nullable UserId + IsAuthenticated)
/// paylaşılan <see cref="IAuditActor"/>'a uyarlar; denetim (audit_log) yazımı kullanıcıyı ("kim")
/// var olan kimlik bağlamından alır, bağlam TAŞINMAZ/DEĞİŞTİRİLMEZ (docs/05 §3.11). Kullanıcı
/// çözülmediyse <see cref="HasUser"/> false, <see cref="UserId"/> <see cref="Guid.Empty"/> döner.
/// </summary>
internal sealed class CurrentUserAuditActor : IAuditActor
{
    private readonly ICurrentUserContext _currentUser;

    public CurrentUserAuditActor(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    public Guid UserId => _currentUser.UserId ?? Guid.Empty;

    public bool HasUser => _currentUser.UserId is { } id && id != Guid.Empty;
}

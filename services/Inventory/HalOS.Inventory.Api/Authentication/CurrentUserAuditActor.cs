using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Abstractions;

namespace HalOS.Inventory.Api.Authentication;

/// <summary>
/// Mevcut <see cref="ICurrentUserContext"/>'i paylaşılan <see cref="IAuditActor"/>'a uyarlar; böylece
/// denetim (audit_log) yazımı kullanıcıyı ("kim") servisin var olan kimlik bağlamından alır ve o
/// bağlam TAŞINMAZ/DEĞİŞTİRİLMEZ (docs/05 §3.11). Anonim/sistem isteğinde <c>UserId == Guid.Empty</c>
/// olduğundan <see cref="HasUser"/> false döner.
/// </summary>
internal sealed class CurrentUserAuditActor : IAuditActor
{
    private readonly ICurrentUserContext _currentUser;

    public CurrentUserAuditActor(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    public Guid UserId => _currentUser.UserId;

    public bool HasUser => _currentUser.UserId != Guid.Empty;
}

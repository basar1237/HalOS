namespace HalOS.BuildingBlocks.Application;

/// <summary>
/// Bir komutu yürüten kullanıcıyı ("kim") sağlayan minik, paylaşılan soyutlama (docs/05 §3.11,
/// docs/03 §6). Her servisin kendi <c>ICurrentUserContext</c>'inden ayrıdır ve ona DOKUNMAZ;
/// yalnız denetim (audit) yazımı için ortak bir arayüz sunar. Api kompozisyon kökünde mevcut
/// kullanıcı bağlamını saran bir adaptöre bağlanır (Identity'de JWT sub claim'inden).
/// </summary>
public interface IAuditActor
{
    /// <summary>Komutu yürüten kullanıcı kimliği; <see cref="HasUser"/> false ise anlamsızdır.</summary>
    Guid UserId { get; }

    /// <summary>Geçerli bağlamda kimliği çözülmüş bir kullanıcı var mı (anonim/sistem değil).</summary>
    bool HasUser { get; }
}

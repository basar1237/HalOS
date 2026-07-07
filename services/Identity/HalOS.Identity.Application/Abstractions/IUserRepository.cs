using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.ValueObjects;

namespace HalOS.Identity.Application.Abstractions;

/// <summary>User aggregate persistence port'u (docs/07 §2 — port arayüzü Application'da).</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// E-posta ile kullanıcı getirir. Login gibi tenant çözümlenmeden önceki akışlar için
    /// <paramref name="ignoreTenantFilter"/> ile global query filter atlanabilir.
    /// </summary>
    Task<User?> GetByEmailAsync(
        Email email,
        bool ignoreTenantFilter = false,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        Email email,
        bool ignoreTenantFilter = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen tenant kapsamında e-postanın kullanımda olup olmadığını kontrol eder.
    /// Register akışı anonimdir (ambient tenant context Guid.Empty olabilir), bu yüzden
    /// kontrol ambient filtreye değil açıkça verilen <paramref name="tenantId"/>'ye göre
    /// yapılır — docs/05'teki (tenant_id, email) tekilliğiyle tutarlı (BK-8).
    /// </summary>
    Task<bool> ExistsByEmailInTenantAsync(
        Guid tenantId,
        Email email,
        CancellationToken cancellationToken = default);

    Task<User?> GetByActiveRefreshTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    void Add(User user);

    void Update(User user);
}

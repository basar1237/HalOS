using HalOS.Identity.Domain.Aggregates;

namespace HalOS.Identity.Application.Abstractions;

/// <summary>Role aggregate persistence port'u.</summary>
public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> ListByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    void AddRange(IEnumerable<Role> roles);
}

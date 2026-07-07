using HalOS.Identity.Domain.Aggregates;

namespace HalOS.Identity.Application.Abstractions;

/// <summary>Tenant aggregate persistence port'u.</summary>
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken = default);

    void Add(Tenant tenant);

    void Update(Tenant tenant);
}

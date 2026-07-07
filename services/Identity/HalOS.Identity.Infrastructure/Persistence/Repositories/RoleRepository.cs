using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Identity.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _dbContext;

    public RoleRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Role>> ListByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.Roles
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);

    public void AddRange(IEnumerable<Role> roles) => _dbContext.Roles.AddRange(roles);
}

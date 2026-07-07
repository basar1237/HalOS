using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Identity.Infrastructure.Persistence.Repositories;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly IdentityDbContext _dbContext;

    public TenantRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _dbContext.Tenants.AnyAsync(t => t.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Tenants.AsNoTracking().ToListAsync(cancellationToken);

    public void Add(Tenant tenant) => _dbContext.Tenants.Add(tenant);

    public void Update(Tenant tenant) => _dbContext.Tenants.Update(tenant);
}

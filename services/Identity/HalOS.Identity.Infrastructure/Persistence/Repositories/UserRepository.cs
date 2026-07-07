using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Domain.Aggregates;
using HalOS.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(
        Email email,
        bool ignoreTenantFilter = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.Include(u => u.RefreshTokens).AsQueryable();
        if (ignoreTenantFilter)
        {
            query = query.IgnoreQueryFilters();
        }

        return query.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(
        Email email,
        bool ignoreTenantFilter = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsQueryable();
        if (ignoreTenantFilter)
        {
            query = query.IgnoreQueryFilters();
        }

        return query.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public Task<bool> ExistsByEmailInTenantAsync(
        Guid tenantId,
        Email email,
        CancellationToken cancellationToken = default)
    {
        // Ambient tenant context (Register anonimdir → Guid.Empty olabilir) yerine açıkça
        // verilen tenantId'ye göre kontrol; global query filter atlanır (docs/05 tekillik).
        return _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
    }

    public Task<User?> GetByActiveRefreshTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        // Refresh akışı tenant çözümlenmeden önce olabilir → filtre atlanır.
        return _dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(
                u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash),
                cancellationToken);
    }

    public void Add(User user) => _dbContext.Users.Add(user);

    public void Update(User user) => _dbContext.Users.Update(user);
}

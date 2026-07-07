using System.Reflection;
using System.Text.Json;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.BuildingBlocks.Infrastructure;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Identity.Infrastructure.Persistence;

/// <summary>
/// Identity servisinin EF Core DbContext'i. <see cref="TenantDbContextBase"/>'ten türer;
/// böylece ITenantOwned entity'lerine tenant_id global query filter'ı (docs/07 §6) ve
/// outbox tablosu (docs/04 §10) otomatik uygulanır.
/// </summary>
public sealed class IdentityDbContext : TenantDbContextBase, IUnitOfWork
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Kaydetmeden hemen önce tüm izlenen aggregate'lerin domain event'lerini outbox'a
    /// yazar; böylece event yayını durum değişikliğiyle aynı transaction'da atomiktir
    /// (docs/04 §10). Handler'lar doğrudan yayın yapmaz (docs/07 §5).
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        WriteDomainEventsToOutbox();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void WriteDomainEventsToOutbox()
    {
        var aggregates = ChangeTracker
            .Entries()
            .Select(e => e.Entity)
            .OfType<AggregateRoot<Guid>>()
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    TenantId = aggregate is ITenantOwned owned ? owned.TenantId : null,
                    Type = domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
                    Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    OccurredOnUtc = domainEvent.OccurredOnUtc
                });
            }

            aggregate.ClearDomainEvents();
        }
    }
}

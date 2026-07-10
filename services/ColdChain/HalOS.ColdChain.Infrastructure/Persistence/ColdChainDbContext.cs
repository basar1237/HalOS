using System.Reflection;
using System.Text.Json;
using HalOS.BuildingBlocks.Domain;
using HalOS.BuildingBlocks.Infrastructure;
using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using HalOS.BuildingBlocks.Application;

namespace HalOS.ColdChain.Infrastructure.Persistence;

/// <summary>
/// ColdChain servisinin EF Core DbContext'i. <see cref="TenantDbContextBase"/>'ten türer; böylece
/// ITenantOwned entity'lerine tenant_id global query filter'ı (docs/07 §6 / BK-8) ve outbox tablosu
/// (docs/04 §10) otomatik uygulanır. Tablolar snake_case (docs/05 §3.4). Inventory DbContext
/// deseniyle birebir.
/// </summary>
public sealed class ColdChainDbContext : TenantDbContextBase, IUnitOfWork
{
    public ColdChainDbContext(DbContextOptions<ColdChainDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public DbSet<ColdStorageUnit> ColdStorageUnits => Set<ColdStorageUnit>();

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Kaydetmeden hemen önce izlenen aggregate'lerin domain event'lerini outbox'a yazar; event yayını
    /// durum değişikliğiyle aynı transaction'da atomiktir (docs/04 §10). Inventory ile aynı desen.
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

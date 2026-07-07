using System.Reflection;
using System.Text.Json;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.BuildingBlocks.Infrastructure;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Integration.Infrastructure.Persistence;

/// <summary>
/// Integration (e-Belge &amp; Yasal Entegrasyon) servisinin EF Core DbContext'i.
/// <see cref="TenantDbContextBase"/>'ten türer; ITenantOwned entity'lerine tenant_id global query
/// filter'ı (docs/07 §6 / BK-8) ve outbox tablosu (docs/04 §10) otomatik uygulanır. Tablolar
/// snake_case (docs/05 §3.4/§3.5). Finance/Sales/Party DbContext deseniyle birebir.
/// </summary>
public sealed class IntegrationDbContext : TenantDbContextBase, IUnitOfWork
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public DbSet<ProducerReceipt> ProducerReceipts => Set<ProducerReceipt>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<HksNotification> HksNotifications => Set<HksNotification>();

    public DbSet<ProducerTaxProfile> ProducerTaxProfiles => Set<ProducerTaxProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Kaydetmeden hemen önce tüm izlenen aggregate'lerin domain event'lerini outbox'a yazar;
    /// böylece event yayını durum değişikliğiyle aynı transaction'da atomiktir (docs/04 §10).
    /// Handler'lar/consumer doğrudan yayın yapmaz (docs/07 §5). Finance/Sales/Party ile aynı desen.
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

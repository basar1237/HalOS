using System.Reflection;
using System.Text.Json;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.BuildingBlocks.Infrastructure;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace HalOS.Sales.Infrastructure.Persistence;

/// <summary>
/// Sales servisinin EF Core DbContext'i. <see cref="TenantDbContextBase"/>'ten türer; böylece
/// ITenantOwned entity'lerine tenant_id global query filter'ı (docs/07 §6 / BK-8) ve outbox
/// tablosu (docs/04 §10) otomatik uygulanır. Tablolar snake_case (docs/05 §3.4/§3.5).
/// </summary>
public sealed class SalesDbContext : TenantDbContextBase, IUnitOfWork
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public DbSet<Consignment> Consignments => Set<Consignment>();

    public DbSet<ConsignmentItem> ConsignmentItems => Set<ConsignmentItem>();

    public DbSet<SaleTransaction> SaleTransactions => Set<SaleTransaction>();

    public DbSet<SaleLine> SaleLines => Set<SaleLine>();

    public DbSet<CommissionCalculation> CommissionCalculations => Set<CommissionCalculation>();

    public DbSet<Deduction> Deductions => Set<Deduction>();

    public DbSet<Settlement> Settlements => Set<Settlement>();

    /// <summary>
    /// Müstahsile-özel oran okuma modeli (read-model) — Party servisinden gelen
    /// <c>ProducerWithholdingProfileChanged</c> ile senkronlanır (docs/02 §6). IRateProvider
    /// satış anında oranları buradan çözer.
    /// </summary>
    public DbSet<ProducerRateProfile> ProducerRateProfiles => Set<ProducerRateProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Kaydetmeden hemen önce tüm izlenen aggregate'lerin domain event'lerini outbox'a yazar;
    /// böylece event yayını durum değişikliğiyle aynı transaction'da atomiktir (docs/04 §10).
    /// Handler'lar doğrudan yayın yapmaz (docs/07 §5). Identity/Party servisleriyle aynı desen.
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

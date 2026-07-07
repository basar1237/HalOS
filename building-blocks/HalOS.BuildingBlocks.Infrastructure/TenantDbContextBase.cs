using System.Linq.Expressions;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace HalOS.BuildingBlocks.Infrastructure;

/// <summary>
/// Base <see cref="DbContext"/> for tenant-scoped services. Applies the mandatory
/// <c>tenant_id</c> global query filter (docs/04 ADR-008, docs/07 §6) to every entity that
/// implements <see cref="ITenantOwned"/>, so reads are automatically restricted to the
/// current tenant resolved from <see cref="ITenantContext"/>. Also maps the shared
/// <see cref="OutboxMessage"/> table for the transactional outbox (docs/04 §10).
/// </summary>
public abstract class TenantDbContextBase : DbContext
{
    private readonly ITenantContext _tenantContext;

    protected TenantDbContextBase(DbContextOptions options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Geçerli isteğin tenant'ı. Global query filter bu property üzerinden okur; filtre
    /// ifadesi DbContext ÖRNEĞİNE referans verdiğinden EF Core değeri her sorguda çalışan
    /// örnekten yeniden değerlendirir. Böylece tek (önbelleğe alınmış) model tüm tenant'lar
    /// için doğru kalır; model önbellek anahtarını tenant'a göre çeşitlendirmeye gerek yoktur
    /// (çok-kiracılı izolasyon, docs/07 §6 / BK-8).
    /// </summary>
    public Guid CurrentTenantId => _tenantContext.TenantId;

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureOutbox(modelBuilder);

        // Apply a tenant global query filter to every ITenantOwned entity type.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(BuildTenantFilter(entityType.ClrType));

            // tenant_id participates in most queries and indexes (docs/05 §6).
            modelBuilder.Entity(entityType.ClrType)
                .HasIndex(nameof(ITenantOwned.TenantId));
        }
    }

    private static void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        // Outbox tablosu/kolonları snake_case (docs/05 §3.4/§3.5); tüm servislerin DB'sinde aynı
        // el-yapımı transactional outbox şeması paylaşılır (docs/04 §10).
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_message");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).HasColumnName("id");
            builder.Property(m => m.TenantId).HasColumnName("tenant_id");
            builder.Property(m => m.Type).HasColumnName("type").IsRequired();
            builder.Property(m => m.Content).HasColumnName("content").IsRequired();
            builder.Property(m => m.OccurredOnUtc).HasColumnName("occurred_on_utc");
            builder.Property(m => m.ProcessedOnUtc).HasColumnName("processed_on_utc");
            builder.Property(m => m.Error).HasColumnName("error");
            // İşlenmemiş mesajları oluşma sırasına göre tarayan dispatch sorgusu için indeks.
            builder.HasIndex(m => m.ProcessedOnUtc).HasDatabaseName("ix_outbox_message_processed_on_utc");
        });
    }

    /// <summary>
    /// Builds <c>e =&gt; e.TenantId == this.CurrentTenantId</c> for the given entity type.
    /// The filter references the DbContext INSTANCE's <see cref="CurrentTenantId"/> property.
    /// EF Core özel olarak query filter içindeki DbContext örnek referanslarını tanır ve
    /// değeri her sorguda çalışan örnekten yeniden okur; bu yüzden ifadeye belirli bir
    /// tenant/servis örneği "bake" edilmez ve tek önbelleğe alınmış model tüm tenant'larda
    /// doğru çalışır (docs/07 §6, model cache key'e gerek yok).
    /// </summary>
    private LambdaExpression BuildTenantFilter(Type entityClrType)
    {
        var parameter = Expression.Parameter(entityClrType, "e");

        var entityTenantId = Expression.Property(parameter, nameof(ITenantOwned.TenantId));

        // Reference this DbContext instance's CurrentTenantId; EF re-evaluates it per query.
        var contextConstant = Expression.Constant(this);
        var currentTenantId = Expression.Property(
            contextConstant,
            nameof(CurrentTenantId));

        var body = Expression.Equal(entityTenantId, currentTenantId);

        return Expression.Lambda(body, parameter);
    }
}

using HalOS.Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>warehouse</c> tablosu eşlemesi (docs/06 S2.1 depo lokasyonu). snake_case kolonlar (docs/07 §3).
/// Kod tenant içinde tekildir: UNIQUE(tenant_id, code). Id domain'de üretilir (Guid.NewGuid — docs/07
/// §3); store tarafından ÜRETİLMEZ. StockItemConfiguration deseniyle birebir.
/// </summary>
internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouse");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(w => w.TenantId).HasColumnName("tenant_id");
        builder.Property(w => w.Name).HasColumnName("name").IsRequired();
        builder.Property(w => w.Code).HasColumnName("code").IsRequired().HasMaxLength(32);
        builder.Property(w => w.IsDefault).HasColumnName("is_default");

        // Kod tenant içinde tekil (docs/06 S2.1): tenant içinde depo kodu benzersiz.
        builder.HasIndex(w => new { w.TenantId, w.Code }).IsUnique();

        // Tenant başına tek varsayılan depo değişmezi (docs/06 S2.1): kısmi tekil indeks — yalnız
        // is_default=true satırlarda benzersizlik zorlanır (PostgreSQL partial unique index). Handler
        // tarafındaki demote mantığı ile çift savunma. HasFilter yalnız relational sağlayıcıda
        // etkilidir; InMemory testler filtresiz davranır, tekillik handler'da doğrulanır.
        builder.HasIndex(w => new { w.TenantId, w.IsDefault })
            .IsUnique()
            .HasFilter("is_default");

        builder.Ignore(w => w.DomainEvents);
    }
}

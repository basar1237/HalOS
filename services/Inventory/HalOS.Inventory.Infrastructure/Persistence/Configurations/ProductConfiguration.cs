using HalOS.Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>product</c> tablosu eşlemesi (docs/05 §3.3). snake_case kolonlar (docs/07 §3). Enum metin olarak
/// saklanır (HasConversion&lt;string&gt; — docs/07 §3). Id domain'de üretilir (Guid.NewGuid);
/// store ÜRETMEZ. WarehouseConfiguration deseniyle birebir.
/// </summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("product");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(p => p.Category).HasColumnName("category").HasMaxLength(100);
        builder.Property(p => p.DefaultUnit)
            .HasColumnName("default_unit")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("is_active");

        // Katalog listeleme/filtre için tenant + ad indeksi (docs/05 §6 tenant-öncelikli).
        builder.HasIndex(p => new { p.TenantId, p.Name });

        builder.Ignore(p => p.DomainEvents);
    }
}

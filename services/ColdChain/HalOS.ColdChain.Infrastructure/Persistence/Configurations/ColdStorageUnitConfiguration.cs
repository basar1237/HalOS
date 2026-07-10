using HalOS.ColdChain.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.ColdChain.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>cold_storage_unit</c> tablosu eşlemesi (docs/04 §6). snake_case kolonlar (docs/07 §3). Sıcaklık
/// eşikleri NUMERIC(6,2) decimal (asla float — BK-2). Okumalar (SensorReading) aggregate'in parçasıdır
/// ve kök tarafından yönetilir (_readings backing field ile kapsülleme korunur). Inventory
/// StockItemConfiguration deseniyle birebir.
/// </summary>
internal sealed class ColdStorageUnitConfiguration : IEntityTypeConfiguration<ColdStorageUnit>
{
    public void Configure(EntityTypeBuilder<ColdStorageUnit> builder)
    {
        builder.ToTable("cold_storage_unit");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(u => u.TenantId).HasColumnName("tenant_id");
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(u => u.MinTempC).HasColumnName("min_temp_c").HasColumnType("numeric(6,2)");
        builder.Property(u => u.MaxTempC).HasColumnName("max_temp_c").HasColumnType("numeric(6,2)");
        builder.Property(u => u.IsActive).HasColumnName("is_active");

        // Okumalar (1:N) — aggregate parçası; kapsülleme _readings backing field ile korunur.
        builder.HasMany(u => u.Readings)
            .WithOne()
            .HasForeignKey(r => r.ColdStorageUnitId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.Readings)
            .HasField("_readings")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Son okuma türetilir (Σ/max okuma) — kalıcı kolon değil.
        builder.Ignore(u => u.LatestReading);
        builder.Ignore(u => u.DomainEvents);
    }
}

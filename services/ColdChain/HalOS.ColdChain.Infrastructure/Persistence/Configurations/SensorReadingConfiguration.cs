using HalOS.ColdChain.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.ColdChain.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>sensor_reading</c> tablosu eşlemesi (docs/04 §6 zaman serisi). APPEND-ONLY. snake_case
/// kolonlar (docs/07 §3). Sıcaklık/nem NUMERIC decimal (BK-2). (tenant_id, cold_storage_unit_id,
/// occurred_at) indeksi zaman-serisi sorguları için (docs/05 §6 deseni).
/// </summary>
internal sealed class SensorReadingConfiguration : IEntityTypeConfiguration<SensorReading>
{
    public void Configure(EntityTypeBuilder<SensorReading> builder)
    {
        builder.ToTable("sensor_reading");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.Property(r => r.ColdStorageUnitId).HasColumnName("cold_storage_unit_id");
        builder.Property(r => r.TemperatureC).HasColumnName("temperature_c").HasColumnType("numeric(6,2)");
        builder.Property(r => r.HumidityPercent).HasColumnName("humidity_percent").HasColumnType("numeric(5,2)");
        builder.Property(r => r.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(r => new { r.TenantId, r.ColdStorageUnitId, r.OccurredAt });
    }
}

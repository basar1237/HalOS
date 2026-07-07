using HalOS.Identity.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Identity.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenant");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(t => t.IsActive).HasColumnName("is_active");
        builder.Property(t => t.CreatedOnUtc).HasColumnName("created_on_utc");

        builder.HasIndex(t => t.Name).IsUnique();

        // Domain event'ler DB'ye eşlenmez (yalnızca in-memory kuyruk).
        builder.Ignore(t => t.DomainEvents);
    }
}

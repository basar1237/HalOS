using HalOS.Identity.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("role");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.Property(r => r.SystemRole).HasColumnName("system_role").HasConversion<string>();
        builder.Property(r => r.Name).HasColumnName("name").IsRequired().HasMaxLength(100);

        builder.HasIndex(r => new { r.TenantId, r.SystemRole }).IsUnique();

        builder.Ignore(r => r.DomainEvents);
    }
}

using HalOS.Identity.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Identity.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscription");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.Plan).HasColumnName("plan").HasConversion<string>();
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(s => s.StartsOnUtc).HasColumnName("starts_on_utc");
        builder.Property(s => s.EndsOnUtc).HasColumnName("ends_on_utc");

        builder.HasIndex(s => s.TenantId);

        builder.Ignore(s => s.DomainEvents);
    }
}

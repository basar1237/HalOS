using HalOS.Finance.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Finance.Infrastructure.Persistence.Configurations;

/// <summary><c>cash_register</c> (docs/11 §3.6). Bakiye türetilir (Σ hareket) → Ignore.</summary>
internal sealed class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("cash_register");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(120);
        builder.Property(r => r.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.CreatedOnUtc).HasColumnName("created_at");

        builder.HasMany(r => r.Movements)
            .WithOne()
            .HasForeignKey(m => m.CashRegisterId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Movements)
            .HasField("_movements")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(r => r.Balance);
        builder.Ignore(r => r.DomainEvents);
    }
}

/// <summary><c>cash_movement</c> — kasa hareketi (tahsil/tediye/virman kalemi).</summary>
internal sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("cash_movement");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.CashRegisterId).HasColumnName("cash_register_id");
        builder.Property(m => m.Direction).HasColumnName("direction").HasConversion<string>().HasMaxLength(10);
        builder.Property(m => m.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
        builder.Property(m => m.Description).HasColumnName("description").HasMaxLength(300);
        builder.Property(m => m.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(m => new { m.TenantId, m.CashRegisterId });
    }
}

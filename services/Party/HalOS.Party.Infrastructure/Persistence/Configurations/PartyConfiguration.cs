using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PartyAggregate = HalOS.Party.Domain.Aggregates.Party;

namespace HalOS.Party.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>party</c> tablosu eşlemesi (docs/05 §3.2). snake_case kolonlar (docs/07 §3). Stopaj profili
/// owned tip olarak iki NUMERIC(7,4) kolona açılır (docs/05 §1 oran ölçeği; float yasak — BK-2).
/// Tenant içinde (tenant_id, tckn) ve (tenant_id, vkn) tekildir (dolu olanlar — docs/02 §3.1).
/// </summary>
internal sealed class PartyConfiguration : IEntityTypeConfiguration<PartyAggregate>
{
    public void Configure(EntityTypeBuilder<PartyAggregate> builder)
    {
        builder.ToTable("party");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.DisplayName).HasColumnName("display_name").IsRequired().HasMaxLength(200);
        builder.Property(p => p.Tckn).HasColumnName("tckn").HasMaxLength(11);
        builder.Property(p => p.Vkn).HasColumnName("vkn").HasMaxLength(10);
        builder.Property(p => p.TaxOffice).HasColumnName("tax_office").HasMaxLength(200);
        builder.Property(p => p.Phone).HasColumnName("phone").HasMaxLength(50);
        builder.Property(p => p.Address).HasColumnName("address").HasMaxLength(1000);
        builder.Property(p => p.KeepsRecords).HasColumnName("keeps_records");
        builder.Property(p => p.IsActive).HasColumnName("is_active");
        builder.Property(p => p.CreatedOnUtc).HasColumnName("created_on_utc");

        // Stopaj/Bağ-Kur profili: owned value object → nullable owned kolonlar (NUMERIC(7,4)).
        builder.OwnsOne(p => p.WithholdingProfile, wp =>
        {
            wp.Property(x => x.AgriWithholdingRate)
                .HasColumnName("agri_withholding_rate")
                .HasColumnType("numeric(7,4)");
            wp.Property(x => x.FarmerSskRate)
                .HasColumnName("farmer_ssk_rate")
                .HasColumnType("numeric(7,4)");
        });

        // Roller aggregate'in parçası (bağlı entity koleksiyonu). Kapsülleme _roles alanı ile korunur.
        builder.HasMany(p => p.Roles)
            .WithOne()
            .HasForeignKey(r => r.PartyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Roles)
            .HasField("_roles")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Tenant içinde TCKN/VKN tekil (dolu olanlar için) — docs/02 §3.1, docs/05 §6.
        // Postgres kısmi (partial) unique index ile NULL'lar tekillikten muaf tutulur.
        builder.HasIndex(p => new { p.TenantId, p.Tckn })
            .IsUnique()
            .HasFilter("tckn IS NOT NULL");

        builder.HasIndex(p => new { p.TenantId, p.Vkn })
            .IsUnique()
            .HasFilter("vkn IS NOT NULL");

        // Domain event'ler DB'ye eşlenmez (yalnızca in-memory kuyruk).
        builder.Ignore(p => p.DomainEvents);
    }
}

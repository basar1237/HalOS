using HalOS.Sales.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Sales.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>producer_rate_profile</c> okuma modeli eşlemesi — Party servisinden gelen
/// <c>ProducerWithholdingProfileChanged</c> ile senkronlanır (docs/02 §6). snake_case kolonlar
/// (docs/07 §3). Oranlar NUMERIC(7,4) (docs/05 §1 oran ölçeği; float yasak — BK-2). Müstahsil
/// referansı ID ile (FK değil — docs/05 §5); tenant içinde (tenant_id, producer_party_id) tekil
/// (docs/05 §6) — consumer upsert bu tekilliğe dayanır.
/// </summary>
internal sealed class ProducerRateProfileConfiguration : IEntityTypeConfiguration<ProducerRateProfile>
{
    public void Configure(EntityTypeBuilder<ProducerRateProfile> builder)
    {
        builder.ToTable("producer_rate_profile");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.ProducerPartyId).HasColumnName("producer_party_id");
        builder.Property(p => p.AgriWithholdingRate)
            .HasColumnName("agri_withholding_rate")
            .HasColumnType("numeric(7,4)");
        builder.Property(p => p.FarmerSskRate)
            .HasColumnName("farmer_ssk_rate")
            .HasColumnType("numeric(7,4)");
        builder.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");

        // Müstahsil başına tek profil: tenant içinde (tenant_id, producer_party_id) tekil.
        builder.HasIndex(p => new { p.TenantId, p.ProducerPartyId }).IsUnique();
    }
}

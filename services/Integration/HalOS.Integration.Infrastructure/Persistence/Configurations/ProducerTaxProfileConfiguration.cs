using HalOS.Integration.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Integration.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>producer_tax_profile</c> tablosu eşlemesi (müstahsil vergi/kayıt okuma modeli; docs/05 §3.2).
/// Party servisinden gelen event ile senkronlanır (docs/02 §6). snake_case kolonlar (docs/07 §3).
/// Oranlar NUMERIC(7,4); kayıt-tutar bayrağı e-MM kararını belirler (BK-4). Bir müstahsil tenant
/// içinde en fazla bir profil satırı → UNIQUE (tenant_id, producer_party_id) (yarış koşuluna karşı
/// gerçek tekillik garantisi; upsert bunun üstüne oturur).
/// </summary>
internal sealed class ProducerTaxProfileConfiguration : IEntityTypeConfiguration<ProducerTaxProfile>
{
    public void Configure(EntityTypeBuilder<ProducerTaxProfile> builder)
    {
        builder.ToTable("producer_tax_profile");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.ProducerPartyId).HasColumnName("producer_party_id");
        builder.Property(p => p.KeepsRecords).HasColumnName("keeps_records");
        builder.Property(p => p.AgriWithholdingRate).HasColumnName("agri_withholding_rate").HasColumnType("numeric(7,4)");
        builder.Property(p => p.FarmerSskRate).HasColumnName("farmer_ssk_rate").HasColumnType("numeric(7,4)");
        builder.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");

        // Müstahsil tenant içinde tekil (docs/05 §3.2): yarış koşuluna karşı DB tekilliği.
        builder.HasIndex(p => new { p.TenantId, p.ProducerPartyId }).IsUnique();
    }
}

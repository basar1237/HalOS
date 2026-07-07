using HalOS.Party.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Party.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>party_role</c> tablosu eşlemesi (docs/05 §3.2). Bir taraf birden çok rol taşıyabilir
/// (docs/02 §3.1). Aynı taraf + rol tipi tekildir. snake_case kolonlar (docs/07 §3).
/// </summary>
internal sealed class PartyRoleConfiguration : IEntityTypeConfiguration<PartyRole>
{
    public void Configure(EntityTypeBuilder<PartyRole> builder)
    {
        builder.ToTable("party_role");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.PartyId).HasColumnName("party_id");
        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.Property(r => r.Type).HasColumnName("type").HasConversion<string>();

        // Aynı tarafa aynı rol iki kez atanamaz.
        builder.HasIndex(r => new { r.PartyId, r.Type }).IsUnique();
    }
}

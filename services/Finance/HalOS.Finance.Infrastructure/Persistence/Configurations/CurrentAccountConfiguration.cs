using HalOS.Finance.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Finance.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>current_account</c> tablosu eşlemesi (docs/05 §3.7, cari 1:1 party). snake_case kolonlar
/// (docs/07 §3). Party referansı ID ile (FK değil — docs/05 §5). Hareketler (AccountEntry)
/// aggregate'in parçasıdır ve kök tarafından yönetilir (_entries alanıyla kapsülleme korunur).
///
/// <c>Balance</c> türetilmiş bir property'dir (Σ hareket, docs/02 §3.4); kalıcı kolon değildir —
/// EF'in onu kolon sanmaması için <c>Ignore</c> edilir. İndeks (tenant_id, party_id) tekil (cari
/// 1:1 party); (tenant_id) global filter için base'de eklenir.
/// </summary>
internal sealed class CurrentAccountConfiguration : IEntityTypeConfiguration<CurrentAccount>
{
    public void Configure(EntityTypeBuilder<CurrentAccount> builder)
    {
        builder.ToTable("current_account");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.PartyId).HasColumnName("party_id");

        // Hareketler (1:N) — aggregate parçası; kapsülleme _entries backing field ile korunur.
        builder.HasMany(a => a.Entries)
            .WithOne()
            .HasForeignKey(e => e.CurrentAccountId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Entries)
            .HasField("_entries")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Bakiye türetilir (Σ hareket, docs/02 §3.4) — kalıcı kolon değil.
        builder.Ignore(a => a.Balance);

        // Cari 1:1 party (docs/05 §3.7): tenant içinde party tekil.
        builder.HasIndex(a => new { a.TenantId, a.PartyId }).IsUnique();

        builder.Ignore(a => a.DomainEvents);
    }
}

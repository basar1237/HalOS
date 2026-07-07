using HalOS.Integration.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Integration.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>receipt_deduction</c> tablosu eşlemesi (e-MM kesinti kalemi; docs/02 §1.3/§3.5). snake_case
/// kolonlar (docs/07 §3). Tür metin (HasConversion&lt;string&gt;); tutar NUMERIC(18,2) (decimal — BK-2).
/// e-MM YALNIZ stopaj + çiftçi Bağ-Kur kalemlerini içerir (komisyon/rüsum/KDV girmez — docs/02 §1.2,
/// BK-1/BK-4). <see cref="ProducerReceipt"/>'in bağlı entity'sidir (kök tarafından yönetilir).
/// </summary>
internal sealed class ReceiptDeductionConfiguration : IEntityTypeConfiguration<ReceiptDeduction>
{
    public void Configure(EntityTypeBuilder<ReceiptDeduction> builder)
    {
        builder.ToTable("receipt_deduction");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.ProducerReceiptId).HasColumnName("producer_receipt_id");
        builder.Property(d => d.TenantId).HasColumnName("tenant_id");
        builder.Property(d => d.Type).HasColumnName("type").HasConversion<string>();
        builder.Property(d => d.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
    }
}

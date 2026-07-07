using HalOS.Finance.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Finance.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>account_entry</c> tablosu eşlemesi (docs/05 §3.7). APPEND-ONLY cari hareket defteri: yön
/// (debit/credit) ve tür (sale/settlement/payment/collection/advance/adjustment) metin kolon
/// (HasConversion&lt;string&gt; — docs/07). Tutar NUMERIC(18,2) (decimal — BK-2). ref_id ilgili
/// satış/ödeme/tahsilat (FK değil — docs/05 §5). İndeks (tenant_id, current_account_id, occurred_at)
/// ekstre/bakiye sorgusu için (docs/05 §6). <c>SignedAmount</c> türetilmiştir — kolon değil.
/// </summary>
internal sealed class AccountEntryConfiguration : IEntityTypeConfiguration<AccountEntry>
{
    public void Configure(EntityTypeBuilder<AccountEntry> builder)
    {
        builder.ToTable("account_entry");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CurrentAccountId).HasColumnName("current_account_id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.Direction).HasColumnName("direction").HasConversion<string>();
        builder.Property(e => e.Type).HasColumnName("entry_type").HasConversion<string>();
        builder.Property(e => e.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
        builder.Property(e => e.RefId).HasColumnName("ref_id");
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at");
        builder.Property(e => e.DueDate).HasColumnName("due_date").HasColumnType("date");

        // Türetilmiş bakiye katkısı — kalıcı kolon değil.
        builder.Ignore(e => e.SignedAmount);

        // Ekstre/bakiye sorgusu: (tenant_id, current_account_id, occurred_at) (docs/05 §6).
        builder.HasIndex(e => new { e.TenantId, e.CurrentAccountId, e.OccurredAt });

        // Idempotency taraması: aynı satışın (ref_id) tekrar işlenmesini bulmak için.
        builder.HasIndex(e => new { e.CurrentAccountId, e.Type, e.RefId });
    }
}

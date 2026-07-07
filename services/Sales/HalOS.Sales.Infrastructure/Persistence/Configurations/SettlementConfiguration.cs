using HalOS.Sales.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Sales.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>settlement</c> tablosu eşlemesi (docs/05 §3.5, satışla 1:1). Net tutar NUMERIC(18,2)
/// (decimal — BK-2); due_date date (15 iş günü — BK-3). İndeks (tenant_id, status, due_date)
/// 15 gün hatırlatma sorgusu için (docs/05 §6). snake_case (docs/07 §3).
/// </summary>
internal sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("settlement");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.SaleTransactionId).HasColumnName("sale_transaction_id");
        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.NetAmount).HasColumnName("net_amount").HasColumnType("numeric(18,2)");
        builder.Property(s => s.DueDate).HasColumnName("due_date").HasColumnType("date");
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<string>();

        builder.HasIndex(s => s.SaleTransactionId).IsUnique();

        // 15 gün ödeme hatırlatma sorgusu (docs/05 §6).
        builder.HasIndex(s => new { s.TenantId, s.Status, s.DueDate });
    }
}

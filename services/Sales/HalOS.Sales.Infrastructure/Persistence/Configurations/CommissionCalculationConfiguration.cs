using HalOS.Sales.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Sales.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>commission_calculation</c> tablosu eşlemesi (docs/05 §3.5, satışla 1:1). Oran NUMERIC(7,4),
/// tutar NUMERIC(18,2) (decimal — BK-2). KDV komisyon üzerine hesaplanır; hakedişten düşülmez (BK-1).
/// </summary>
internal sealed class CommissionCalculationConfiguration : IEntityTypeConfiguration<CommissionCalculation>
{
    public void Configure(EntityTypeBuilder<CommissionCalculation> builder)
    {
        builder.ToTable("commission_calculation");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.SaleTransactionId).HasColumnName("sale_transaction_id");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.CommissionRate).HasColumnName("commission_rate").HasColumnType("numeric(7,4)");
        builder.Property(c => c.CommissionAmount).HasColumnName("commission_amount").HasColumnType("numeric(18,2)");
        builder.Property(c => c.VatRate).HasColumnName("vat_rate").HasColumnType("numeric(7,4)");
        builder.Property(c => c.VatAmount).HasColumnName("vat_amount").HasColumnType("numeric(18,2)");

        // Satışla 1:1 (docs/05 §3.5).
        builder.HasIndex(c => c.SaleTransactionId).IsUnique();
    }
}

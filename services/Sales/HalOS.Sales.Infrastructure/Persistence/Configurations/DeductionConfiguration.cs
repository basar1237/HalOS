using HalOS.Sales.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Sales.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>deduction</c> tablosu eşlemesi (docs/05 §3.5). Komisyon/stopaj/Bağ-Kur/rüsum/KDV AYRI
/// satırlar (docs/02 §7). Oran NUMERIC(7,4), tutar NUMERIC(18,2) (decimal — BK-2). snake_case.
/// </summary>
internal sealed class DeductionConfiguration : IEntityTypeConfiguration<Deduction>
{
    public void Configure(EntityTypeBuilder<Deduction> builder)
    {
        builder.ToTable("deduction");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.SaleTransactionId).HasColumnName("sale_transaction_id");
        builder.Property(d => d.TenantId).HasColumnName("tenant_id");
        builder.Property(d => d.Type).HasColumnName("type").HasConversion<string>();
        builder.Property(d => d.Rate).HasColumnName("rate").HasColumnType("numeric(7,4)");
        builder.Property(d => d.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
    }
}

using HalOS.Sales.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Sales.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>sale_line</c> tablosu eşlemesi (docs/05 §3.5). Miktar NUMERIC(18,3); birim fiyat/tutar
/// NUMERIC(18,2) (decimal — BK-2). Ürün ID ile (FK değil — docs/05 §5). snake_case (docs/07 §3).
/// </summary>
internal sealed class SaleLineConfiguration : IEntityTypeConfiguration<SaleLine>
{
    public void Configure(EntityTypeBuilder<SaleLine> builder)
    {
        builder.ToTable("sale_line");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.SaleTransactionId).HasColumnName("sale_transaction_id");
        builder.Property(l => l.TenantId).HasColumnName("tenant_id");
        builder.Property(l => l.ProductId).HasColumnName("product_id");
        builder.Property(l => l.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,3)");
        builder.Property(l => l.Unit).HasColumnName("unit").HasConversion<string>();
        builder.Property(l => l.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(18,2)");
        builder.Property(l => l.LineAmount).HasColumnName("line_amount").HasColumnType("numeric(18,2)");
    }
}

using HalOS.Sales.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Sales.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>consignment_item</c> tablosu eşlemesi (docs/05 §3.4). Miktar NUMERIC(18,3); ürün ID ile
/// (FK değil — docs/05 §5). snake_case kolonlar (docs/07 §3).
/// </summary>
internal sealed class ConsignmentItemConfiguration : IEntityTypeConfiguration<ConsignmentItem>
{
    public void Configure(EntityTypeBuilder<ConsignmentItem> builder)
    {
        builder.ToTable("consignment_item");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.ConsignmentId).HasColumnName("consignment_id");
        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.ProductId).HasColumnName("product_id");
        builder.Property(i => i.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,3)");
        builder.Property(i => i.Unit).HasColumnName("unit").HasConversion<string>();
    }
}

using HalOS.Integration.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Integration.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>product_passport</c> tablosu eşlemesi (künye; docs/02 §3.5, docs/05). snake_case kolonlar
/// (docs/07 §3). Miktar NUMERIC(18,3) (docs/05 §3.4). Status metin (HasConversion&lt;string&gt;).
/// Taraf/parti/ürün referansları ID ile (FK değil — docs/05 §5).
///
/// Idempotency DB'de zorlanır: bir mal geliş kalemi (consignment_item_id) tenant içinde en fazla BİR
/// künye üretir → UNIQUE (tenant_id, consignment_item_id). Consumer'ın ön-kontrolüne ek olarak yarış
/// koşuluna karşı gerçek garanti. ProducerReceipt/Invoice config deseniyle birebir.
/// </summary>
internal sealed class ProductPassportConfiguration : IEntityTypeConfiguration<ProductPassport>
{
    public void Configure(EntityTypeBuilder<ProductPassport> builder)
    {
        builder.ToTable("product_passport");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.ConsignmentId).HasColumnName("consignment_id");
        builder.Property(p => p.ConsignmentItemId).HasColumnName("consignment_item_id");
        builder.Property(p => p.ProductId).HasColumnName("product_id");
        builder.Property(p => p.ProducerPartyId).HasColumnName("producer_party_id");
        builder.Property(p => p.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,3)");
        builder.Property(p => p.UnitCode).HasColumnName("unit_code").HasMaxLength(32);
        builder.Property(p => p.ReceivedAt).HasColumnName("received_at");
        builder.Property(p => p.PassportCode).HasColumnName("passport_code").HasMaxLength(32);
        builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>();

        // Idempotency DB kısıtı: mal geliş kalemi başına tek künye (docs/04 §5).
        builder.HasIndex(p => new { p.TenantId, p.ConsignmentItemId }).IsUnique();

        builder.Ignore(p => p.DomainEvents);
    }
}

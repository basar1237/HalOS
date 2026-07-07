using HalOS.Integration.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Integration.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>hks_notification</c> tablosu eşlemesi (HKS bildirimi; docs/02 §3.5, docs/05). snake_case kolonlar
/// (docs/07 §3). Tutarlar NUMERIC(18,2) (decimal — BK-2). Brüt + komisyon + hal rüsumu AYRI kolonlar
/// (docs/02 §7). Durum metin (HasConversion&lt;string&gt;). Taraf/satış referansları ID ile (FK değil
/// — docs/05 §5).
///
/// Idempotency DB'de zorlanır: bir satış (sale_transaction_id) tenant içinde en fazla BİR HKS bildirimi
/// üretir → UNIQUE (tenant_id, sale_transaction_id). ProducerReceiptConfiguration deseniyle birebir.
/// </summary>
internal sealed class HksNotificationConfiguration : IEntityTypeConfiguration<HksNotification>
{
    public void Configure(EntityTypeBuilder<HksNotification> builder)
    {
        builder.ToTable("hks_notification");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id).HasColumnName("id");
        builder.Property(n => n.TenantId).HasColumnName("tenant_id");
        builder.Property(n => n.SaleTransactionId).HasColumnName("sale_transaction_id");
        builder.Property(n => n.BuyerPartyId).HasColumnName("buyer_party_id");
        builder.Property(n => n.ProducerPartyId).HasColumnName("producer_party_id");
        builder.Property(n => n.NotifiedDate).HasColumnName("notified_date");
        builder.Property(n => n.GrossAmount).HasColumnName("gross_amount").HasColumnType("numeric(18,2)");
        builder.Property(n => n.CommissionAmount).HasColumnName("commission_amount").HasColumnType("numeric(18,2)");
        builder.Property(n => n.MarketFeeAmount).HasColumnName("market_fee_amount").HasColumnType("numeric(18,2)");
        builder.Property(n => n.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(64);
        builder.Property(n => n.Status).HasColumnName("status").HasConversion<string>();

        // Idempotency DB kısıtı: satış başına tek HKS bildirimi (docs/04 §5).
        builder.HasIndex(n => new { n.TenantId, n.SaleTransactionId }).IsUnique();

        builder.Ignore(n => n.DomainEvents);
    }
}

using HalOS.Integration.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Integration.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>invoice</c> tablosu eşlemesi (e-Fatura HAL; docs/02 §1.2/§3.5, docs/05). snake_case kolonlar
/// (docs/07 §3). Tutarlar NUMERIC(18,2) (decimal — BK-2). Senaryo/tür/durum metin
/// (HasConversion&lt;string&gt;). Taraf/satış referansları ID ile (FK değil — docs/05 §5).
///
/// Idempotency DB'de zorlanır: bir satış (sale_transaction_id) tenant içinde en fazla BİR e-Fatura
/// üretir → UNIQUE (tenant_id, sale_transaction_id). ProducerReceiptConfiguration deseniyle birebir.
/// </summary>
internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoice");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.SaleTransactionId).HasColumnName("sale_transaction_id");
        builder.Property(i => i.BuyerPartyId).HasColumnName("buyer_party_id");
        builder.Property(i => i.IssueDate).HasColumnName("issue_date");
        builder.Property(i => i.Scenario).HasColumnName("scenario").HasConversion<string>();
        builder.Property(i => i.Type).HasColumnName("type").HasConversion<string>();
        builder.Property(i => i.CommissionAmount).HasColumnName("commission_amount").HasColumnType("numeric(18,2)");
        builder.Property(i => i.CommissionVatAmount).HasColumnName("commission_vat_amount").HasColumnType("numeric(18,2)");
        builder.Property(i => i.TotalAmount).HasColumnName("total_amount").HasColumnType("numeric(18,2)");
        builder.Property(i => i.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(64);
        builder.Property(i => i.Status).HasColumnName("status").HasConversion<string>();

        // Idempotency DB kısıtı: satış başına tek e-Fatura (docs/04 §5).
        builder.HasIndex(i => new { i.TenantId, i.SaleTransactionId }).IsUnique();

        builder.Ignore(i => i.DomainEvents);
    }
}

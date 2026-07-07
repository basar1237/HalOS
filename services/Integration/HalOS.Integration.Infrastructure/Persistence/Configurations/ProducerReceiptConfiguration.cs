using HalOS.Integration.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Integration.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>producer_receipt</c> tablosu eşlemesi (e-MM; docs/02 §3.5, docs/05). snake_case kolonlar
/// (docs/07 §3). Tutarlar NUMERIC(18,2) (decimal — BK-2). Taraf/satış referansları ID ile (FK değil
/// — docs/05 §5). Kesinti kalemleri (ReceiptDeduction) aggregate'in parçasıdır; kök tarafından
/// yönetilir (_deductions backing field ile kapsülleme korunur).
///
/// Idempotency DB'de zorlanır: bir satış (sale_transaction_id) tenant içinde en fazla BİR e-MM üretir
/// → UNIQUE (tenant_id, sale_transaction_id). Bu, consumer'ın ön-kontrolüne ek olarak yarış koşuluna
/// (eşzamanlı teslimat) karşı gerçek garantidir.
/// </summary>
internal sealed class ProducerReceiptConfiguration : IEntityTypeConfiguration<ProducerReceipt>
{
    public void Configure(EntityTypeBuilder<ProducerReceipt> builder)
    {
        builder.ToTable("producer_receipt");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.Property(r => r.SaleTransactionId).HasColumnName("sale_transaction_id");
        builder.Property(r => r.ProducerPartyId).HasColumnName("producer_party_id");
        builder.Property(r => r.BuyerPartyId).HasColumnName("buyer_party_id");
        builder.Property(r => r.IssueDate).HasColumnName("issue_date");
        builder.Property(r => r.GrossAmount).HasColumnName("gross_amount").HasColumnType("numeric(18,2)");
        builder.Property(r => r.AgriWithholdingAmount).HasColumnName("agri_withholding_amount").HasColumnType("numeric(18,2)");
        builder.Property(r => r.FarmerSskAmount).HasColumnName("farmer_ssk_amount").HasColumnType("numeric(18,2)");
        builder.Property(r => r.NetPayable).HasColumnName("net_payable").HasColumnType("numeric(18,2)");
        builder.Property(r => r.ReceiptNumber).HasColumnName("receipt_number").HasMaxLength(64);
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>();

        // Kesinti kalemleri (1:N) — aggregate parçası.
        builder.HasMany(r => r.Deductions)
            .WithOne()
            .HasForeignKey(d => d.ProducerReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Deductions)
            .HasField("_deductions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Idempotency DB kısıtı: satış başına tek e-MM (docs/04 §5).
        builder.HasIndex(r => new { r.TenantId, r.SaleTransactionId }).IsUnique();

        builder.Ignore(r => r.DomainEvents);
    }
}

using HalOS.Sales.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Sales.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>sale_transaction</c> tablosu eşlemesi (docs/05 §3.5) — ÇEKİRDEK. snake_case kolonlar
/// (docs/07 §3). Para NUMERIC(18,2) (decimal — BK-2). Taraf/kaynak referansları ID ile (FK değil —
/// docs/05 §5). Satırlar/kesintiler/komisyon/hakediş aggregate'in parçasıdır ve kök tarafından
/// yönetilir (kapsülleme _lines/_deductions alanlarıyla korunur).
///
/// İndeksler (docs/05 §6): (tenant_id, sold_at), (tenant_id, buyer_party_id), (tenant_id, status).
/// operation_id offline idempotency için tenant içinde tekil (docs/04 §5).
/// </summary>
internal sealed class SaleTransactionConfiguration : IEntityTypeConfiguration<SaleTransaction>
{
    public void Configure(EntityTypeBuilder<SaleTransaction> builder)
    {
        builder.ToTable("sale_transaction");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.BuyerPartyId).HasColumnName("buyer_party_id");
        builder.Property(s => s.ProducerPartyId).HasColumnName("producer_party_id");
        builder.Property(s => s.ConsignmentId).HasColumnName("consignment_id");
        builder.Property(s => s.SoldAt).HasColumnName("sold_at");
        builder.Property(s => s.GrossAmount).HasColumnName("gross_amount").HasColumnType("numeric(18,2)");
        builder.Property(s => s.IsWithinMarket).HasColumnName("is_within_market");
        builder.Property(s => s.Term).HasColumnName("term").HasConversion<string>();
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(s => s.OperationId).HasColumnName("operation_id");
        builder.Property(s => s.IsCancelled).HasColumnName("is_cancelled");
        builder.Property(s => s.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");
        builder.Property(s => s.CreatedOnUtc).HasColumnName("created_at");

        // Satırlar (1:N) — aggregate parçası.
        builder.HasMany(s => s.Lines)
            .WithOne()
            .HasForeignKey(l => l.SaleTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Lines)
            .HasField("_lines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Kesintiler (1:N) — aggregate parçası.
        builder.HasMany(s => s.Deductions)
            .WithOne()
            .HasForeignKey(d => d.SaleTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Deductions)
            .HasField("_deductions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Komisyon hesabı (1:1) — aggregate parçası.
        builder.HasOne(s => s.CommissionCalculation)
            .WithOne()
            .HasForeignKey<CommissionCalculation>(c => c.SaleTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.CommissionCalculation)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        // Hakediş (1:1) — aggregate parçası.
        builder.HasOne(s => s.Settlement)
            .WithOne()
            .HasForeignKey<Settlement>(x => x.SaleTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Settlement)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        // İndeksler (docs/05 §6).
        builder.HasIndex(s => new { s.TenantId, s.SoldAt });
        builder.HasIndex(s => new { s.TenantId, s.BuyerPartyId });
        builder.HasIndex(s => new { s.TenantId, s.Status });

        // Offline idempotency: aynı operationId tenant içinde tekil (docs/04 §5).
        builder.HasIndex(s => new { s.TenantId, s.OperationId }).IsUnique();

        builder.Ignore(s => s.DomainEvents);
    }
}

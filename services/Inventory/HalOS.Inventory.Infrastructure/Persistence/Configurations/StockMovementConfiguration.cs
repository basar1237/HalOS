using HalOS.Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>stock_movement</c> tablosu eşlemesi (docs/02 §115). APPEND-ONLY stok hareket defteri: tür
/// (intake/sale-out/spoilage/adjustment) metin kolon (HasConversion&lt;string&gt; — docs/07). İşaretli
/// miktar NUMERIC(18,3) (decimal — asla float, BK-2). ref_id kaynak (consignment_item_id /
/// sale_line_id — FK değil, docs/05 §5). İndeks (tenant_id, stock_item_id, occurred_at) döküm/kalan
/// sorgusu için (docs/05 §6). Idempotency indeksi (tenant_id, stock_item_id, kind, ref_id) TEKİL —
/// aynı kaynak hareket iki kez işlenemez (docs/04 §5). Finance AccountEntryConfiguration deseniyle birebir.
/// </summary>
internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movement");
        builder.HasKey(m => m.Id);

        // Id domain'de üretilir (Guid.NewGuid, docs/07 §3); store tarafından ÜRETİLMEZ — böylece
        // önceden yüklenmiş bir stok kalemine eklenen yeni hareket doğru şekilde Added (Insert) işlenir.
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(m => m.StockItemId).HasColumnName("stock_item_id");
        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.Kind).HasColumnName("kind").HasConversion<string>();
        builder.Property(m => m.SignedQuantity).HasColumnName("signed_quantity").HasColumnType("numeric(18,3)");
        builder.Property(m => m.RefId).HasColumnName("ref_id");
        builder.Property(m => m.Reason).HasColumnName("reason");
        builder.Property(m => m.OccurredAt).HasColumnName("occurred_at");

        // Döküm/kalan sorgusu: (tenant_id, stock_item_id, occurred_at) (docs/05 §6).
        builder.HasIndex(m => new { m.TenantId, m.StockItemId, m.OccurredAt });

        // Idempotency: aynı kaynak (kind, ref_id) stok kalemi içinde tekil (docs/04 §5). ref_id null
        // (fire/düzeltme) olabileceğinden filtreli tekil indeks — Postgres null'ları benzersiz saymaz
        // ama açık filtre kaynak-referanslı hareketleri (intake/sale-out) korur.
        builder.HasIndex(m => new { m.TenantId, m.StockItemId, m.Kind, m.RefId })
            .IsUnique()
            .HasFilter("ref_id IS NOT NULL");
    }
}

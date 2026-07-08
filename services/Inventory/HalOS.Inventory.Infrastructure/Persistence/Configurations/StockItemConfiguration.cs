using HalOS.Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HalOS.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>stock_item</c> tablosu eşlemesi (docs/02 §115). snake_case kolonlar (docs/07 §3). Ürün
/// referansı ID ile (FK değil — docs/05 §5). Hareketler (StockMovement) aggregate'in parçasıdır ve
/// kök tarafından yönetilir (_movements alanıyla kapsülleme korunur).
///
/// <c>QuantityOnHand</c> türetilmiş bir property'dir (Σ hareket, docs/02 §115); kalıcı kolon değildir —
/// EF'in onu kolon sanmaması için <c>Ignore</c> edilir. İndeks (tenant_id, product_id) TEKİL (tenant +
/// ürün başına tek stok kalemi). Finance CurrentAccountConfiguration deseniyle birebir.
/// </summary>
internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_item");
        builder.HasKey(i => i.Id);

        // Id domain'de üretilir (Guid.NewGuid, docs/07 §3); store tarafından ÜRETİLMEZ. Aksi halde
        // EF, önceden yüklenmiş bir aggregate'e eklenen yeni bir hareketi (client-set Guid'li) mevcut
        // satır sanıp Modified işler (özellikle InMemory sağlayıcıda hata → doğru davranış: Added).
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.ProductId).HasColumnName("product_id");

        // Hareketler (1:N) — aggregate parçası; kapsülleme _movements backing field ile korunur.
        builder.HasMany(i => i.Movements)
            .WithOne()
            .HasForeignKey(m => m.StockItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Movements)
            .HasField("_movements")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Kalan türetilir (Σ hareket, docs/02 §115) — kalıcı kolon değil.
        builder.Ignore(i => i.QuantityOnHand);

        // Tenant + ürün başına tek stok kalemi (docs/02 §115): tenant içinde ürün tekil.
        builder.HasIndex(i => new { i.TenantId, i.ProductId }).IsUnique();

        builder.Ignore(i => i.DomainEvents);
    }
}

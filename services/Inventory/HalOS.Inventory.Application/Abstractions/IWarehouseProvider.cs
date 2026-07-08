using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Abstractions;

/// <summary>
/// Olay-güdümlü stok girişi/çıkışında (Consignment/Sale) hedef depoyu sağlar (docs/06 S2.1). Olaylar
/// warehouse taşımadığından stok kaydı tenant'ın VARSAYILAN deposuna yazılır; varsayılan depo yoksa
/// lazım olunca "Merkez Depo" (Code="MERKEZ", IsDefault=true) oluşturulur. Consumer içinde HTTP/dış
/// sorgu yok (docs/07 §5); yalnızca kendi DbContext'i üzerinden çalışır.
/// </summary>
public interface IWarehouseProvider
{
    /// <summary>
    /// Verilen tenant'ın varsayılan deposunu getirir; yoksa "Merkez Depo" (Code="MERKEZ",
    /// IsDefault=true) oluşturup repository'ye ekler ve döner. Kaydetme (SaveChanges) çağıranın
    /// unit-of-work'üne bırakılır — aynı transaction'da atomik kalır (docs/04 §10).
    /// </summary>
    Task<Warehouse> GetOrCreateDefaultAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Infrastructure.Persistence;

/// <summary>
/// <see cref="IWarehouseProvider"/> uygulaması (docs/06 S2.1). Olay-güdümlü giriş/çıkışta hedef depoyu
/// sağlar: tenant'ın varsayılan deposu varsa onu döner, yoksa "Merkez Depo" (Code="MERKEZ",
/// IsDefault=true) oluşturup repository'ye ekler. Kaydetme (SaveChanges) çağıranın unit-of-work'üne
/// bırakılır (aynı transaction'da atomik — docs/04 §10). Tenant global filter, consumer scope'unda
/// ambient tenant ile çalıştığından <see cref="IWarehouseRepository.GetDefaultAsync"/> doğru tenant'ın
/// deposunu bulur (docs/07 §6 / BK-8).
/// </summary>
internal sealed class WarehouseProvider : IWarehouseProvider
{
    /// <summary>Varsayılan depo yoksa oluşturulan merkez deponun kodu (docs/06 S2.1 notu).</summary>
    private const string DefaultWarehouseCode = "MERKEZ";

    /// <summary>Varsayılan depo yoksa oluşturulan merkez deponun görünen adı.</summary>
    private const string DefaultWarehouseName = "Merkez Depo";

    private readonly IWarehouseRepository _warehouses;

    public WarehouseProvider(IWarehouseRepository warehouses)
    {
        _warehouses = warehouses;
    }

    public async Task<Warehouse> GetOrCreateDefaultAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _warehouses.GetDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        // Varsayılan depo yok → "Merkez Depo" oluştur (Create hatasız: sabit geçerli ad/kod).
        var warehouse = Warehouse.Create(tenantId, DefaultWarehouseName, DefaultWarehouseCode, isDefault: true).Value;
        _warehouses.Add(warehouse);
        return warehouse;
    }
}

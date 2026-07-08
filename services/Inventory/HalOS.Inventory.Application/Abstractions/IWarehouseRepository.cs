using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Abstractions;

/// <summary>
/// Warehouse aggregate persistence port'u (docs/06 S2.1 depo lokasyonu). Tüm sorgular tenant global
/// query filter'a tabidir (BK-8). Kod tenant içinde tekildir (UNIQUE(tenant_id, code)).
/// IStockItemRepository deseniyle birebir.
/// </summary>
public interface IWarehouseRepository
{
    /// <summary>Depoyu kimliğiyle getirir (tenant filtreli); yoksa null.</summary>
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Verilen kodlu depoyu getirir (tenant içinde tekil); yoksa null.</summary>
    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant'ın varsayılan deposunu getirir; yoksa null. Değişmez gereği tenant başına en fazla bir
    /// varsayılan depo bulunur; savunma amaçlı (bozuk veri) DETERMİNİSTİK sıralama (koda göre) uygulanır
    /// ki birden çok varsayılan olsa bile hep aynı depo dönsün.
    /// </summary>
    Task<Warehouse?> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant'ın varsayılan işaretli TÜM depolarını getirir (tekillik değişmezini korumak için yeni bir
    /// varsayılan atanmadan önce eski varsayılan(lar)ı düşürmekte kullanılır; docs/06 S2.1).
    /// </summary>
    Task<IReadOnlyList<Warehouse>> ListDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>Verilen kodlu bir deponun tenant içinde var olup olmadığını döner.</summary>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Tenant'ın tüm depolarını ada göre sıralı listeler (tenant filtreli).</summary>
    Task<IReadOnlyList<Warehouse>> ListAsync(CancellationToken cancellationToken = default);

    void Add(Warehouse warehouse);

    void Update(Warehouse warehouse);
}

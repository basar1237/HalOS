namespace HalOS.Inventory.Api.Contracts;

/// <summary>
/// Fire (zayiat) kaydı isteği (docs/03 M9 / BK-7; docs/02 §57 Fire=Spoilage). Ürün + miktar +
/// gerekçe. BK-7: fire mevcut stoğu aşamaz (kalan negatif olamaz) — domain'de doğrulanır.
/// </summary>
public sealed record RecordSpoilageRequest(
    Guid ProductId,
    decimal Quantity,
    string Reason,
    DateTime OccurredAt);

/// <summary>
/// Depo oluşturma isteği (docs/06 S2.1 depo lokasyonu). Ad + tenant içinde tekil kod + varsayılan mı.
/// </summary>
public sealed record CreateWarehouseRequest(
    string Name,
    string Code,
    bool IsDefault);

/// <summary>
/// Yeniden-sipariş eşiği ayarlama isteği (docs/06 S2.1 stok uyarıları). Ürün + eşik (null: kaldır).
/// </summary>
public sealed record SetReorderThresholdRequest(
    Guid ProductId,
    decimal? ReorderThreshold);

/// <summary>Yeni ürün oluşturma isteği (docs/03 M2; docs/05 §3.3). Ad + kategori(ops.) + varsayılan birim.</summary>
public sealed record CreateProductRequest(
    string Name,
    string? Category,
    HalOS.Inventory.Domain.Enums.UnitOfMeasure DefaultUnit);

/// <summary>Ürün güncelleme isteği (docs/03 M2).</summary>
public sealed record UpdateProductRequest(
    string Name,
    string? Category,
    HalOS.Inventory.Domain.Enums.UnitOfMeasure DefaultUnit);

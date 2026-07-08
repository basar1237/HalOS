using HalOS.BuildingBlocks.Contracts;
using HalOS.BuildingBlocks.Domain;

namespace HalOS.Inventory.Domain.Events;

/// <summary>
/// Bir ürün için fire (zayiat) kaydedildiğinde yayınlanır (docs/02 §57 Fire=Spoilage ürün bazlı %;
/// §237 SpoilageRecorded → Finans/AI). Finans fire maliyetini/mahsubu, AI ise fire oranı analizini
/// (ürün bazlı %) bu event ile dinler (docs/02 §237). Event adı PascalCase geçmiş zaman (docs/07 §3).
///
/// <see cref="ITenantScopedEvent"/>'i uygular: broker üzerinden geçerken tenant bağlamı mesajın
/// kendisiyle taşınır, consumer <see cref="TenantId"/>'yi ambient tenant'a set eder (docs/07 §6 / BK-8).
/// </summary>
/// <param name="StockItemId">Fire kaydedilen stok kaleminin kimliği.</param>
/// <param name="TenantId">Firenin bağlı olduğu işletme (tenant) — ITenantScopedEvent (BK-8).</param>
/// <param name="ProductId">Ürün referansı (FK değil, docs/05 §5) — AI ürün bazlı fire oranı için.</param>
/// <param name="Quantity">Fire miktarı (pozitif, NUMERIC(18,3); decimal — asla float, BK-2).</param>
/// <param name="Reason">Fire gerekçesi (çürüme/ezilme vb.).</param>
/// <param name="OccurredOnUtc">Olayın gerçekleştiği an (UTC).</param>
public sealed record SpoilageRecorded(
    Guid StockItemId,
    Guid TenantId,
    Guid ProductId,
    decimal Quantity,
    string Reason,
    DateTime OccurredOnUtc) : IDomainEvent, ITenantScopedEvent;

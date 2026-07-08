using HalOS.BuildingBlocks.Contracts;
using HalOS.BuildingBlocks.Domain;

namespace HalOS.Inventory.Domain.Events;

/// <summary>
/// Bir stok kaleminin eldeki miktarı yeniden-sipariş eşiğine (<c>ReorderThreshold</c>) VEYA altına
/// indiğinde yayınlanır (docs/06 S2.1 stok uyarıları). İleride Bildirim/AI servisleri bu event ile
/// düşük stok uyarısı üretir; el-yapımı outbox üzerinden yayılır (docs/04 §10). Event adı PascalCase
/// geçmiş zaman (docs/07 §3).
///
/// <see cref="ITenantScopedEvent"/>'i uygular: broker üzerinden geçerken tenant bağlamı mesajın
/// kendisiyle taşınır (docs/07 §6 / BK-8). <see cref="SpoilageRecorded"/> deseniyle birebir.
/// </summary>
/// <param name="StockItemId">Uyarı üreten stok kaleminin kimliği.</param>
/// <param name="TenantId">Kalemin bağlı olduğu işletme (tenant) — ITenantScopedEvent (BK-8).</param>
/// <param name="WarehouseId">Kalemin bulunduğu depo (docs/06 S2.1 depo lokasyonu).</param>
/// <param name="ProductId">Ürün referansı (FK değil, docs/05 §5).</param>
/// <param name="QuantityOnHand">Uyarı anındaki eldeki miktar (NUMERIC(18,3); decimal — BK-2).</param>
/// <param name="ReorderThreshold">Aşılan yeniden-sipariş eşiği (NUMERIC(18,3); decimal — BK-2).</param>
/// <param name="OccurredOnUtc">Olayın gerçekleştiği an (UTC).</param>
public sealed record LowStockAlerted(
    Guid StockItemId,
    Guid TenantId,
    Guid WarehouseId,
    Guid ProductId,
    decimal QuantityOnHand,
    decimal ReorderThreshold,
    DateTime OccurredOnUtc) : IDomainEvent, ITenantScopedEvent;

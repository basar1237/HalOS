using HalOS.BuildingBlocks.Domain;

namespace HalOS.Sales.Domain.Events;

/// <summary>
/// Bir satış iptal edildiğinde yayınlanır. Tamamlanmış satış SİLİNMEZ; iptal ters kayıt/flag
/// ile yapılır ve denetim izi korunur (docs/03 §4 BK-9). Finance ters cari kaydı, e-Belge iade/
/// düzeltme belgesi için dinler. Event adı PascalCase geçmiş zaman (docs/07 §3).
/// </summary>
public sealed record SaleCancelled(
    Guid SaleTransactionId,
    Guid TenantId,
    string Reason,
    DateTime OccurredOnUtc) : IDomainEvent;

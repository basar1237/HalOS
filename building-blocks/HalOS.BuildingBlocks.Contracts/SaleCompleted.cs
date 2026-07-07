using HalOS.BuildingBlocks.Domain;

namespace HalOS.BuildingBlocks.Contracts;

/// <summary>
/// Bir satış tamamlanıp kesinti/hakediş hesaplandığında yayınlanır (docs/02 §6: Satış →
/// Finans, e-Belge, Bildirim, Stok, AI). Çekirdek servisler-arası entegrasyon event'idir; bu
/// yüzden paylaşılan <c>Contracts</c> projesinde yaşar ve tüketen servisler (Finance cari,
/// e-Belge e-MM) tekrar hesap yapmadan davranabilsin diye net hakediş ve toplam kesinti
/// alanlarını taşır. Event adı PascalCase geçmiş zaman (docs/07 §3).
///
/// <see cref="ITenantScopedEvent"/>'i uygular: broker üzerinden geçerken tenant bağlamı
/// mesajın kendisiyle taşınır, consumer <see cref="TenantId"/>'yi ambient tenant'a set eder
/// (docs/07 §6 / BK-8).
/// </summary>
public sealed record SaleCompleted(
    Guid SaleTransactionId,
    Guid TenantId,
    Guid BuyerPartyId,
    Guid ProducerPartyId,
    DateTime SoldAt,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal TotalDeductions,
    decimal NetAmount,
    DateTime SettlementDueDate,
    DateTime OccurredOnUtc) : IDomainEvent, ITenantScopedEvent;

using HalOS.BuildingBlocks.Domain;

namespace HalOS.Integration.Domain.Events;

/// <summary>
/// Bir satışın HKS'e bildirimi başarıyla gönderildiğinde yayınlanır (docs/02 §3.5 / §6 <c>HksNotified</c>;
/// docs/03 M8 / BK-4: "her satış HKS'e bildirilir"). Satış servisi (belge durumu), Bildirim ve AI bunu
/// dinleyebilir (docs/02 §6). İç kullanım domain event'idir (Integration.Domain.Events); bildirim
/// gönderilince el-yapımı outbox'a atomik yazılır ve OutboxDispatcher ile yayınlanır (docs/04 §10);
/// handler/consumer doğrudan yayın yapmaz (docs/07 §5). Event adı PascalCase geçmiş zaman (docs/07 §3).
/// <see cref="ProducerReceiptIssued"/> deseniyle birebir.
/// </summary>
/// <param name="HksNotificationId">Gönderilen HKS bildiriminin kimliği.</param>
/// <param name="TenantId">Belgenin ait olduğu işletme (tenant) — outbox tenant izolasyonu (BK-8).</param>
/// <param name="SaleTransactionId">Bildirimin kaynağı satış (idempotency anahtarı).</param>
/// <param name="ReferenceNumber">HKS'in döndürdüğü bildirim referans numarası.</param>
/// <param name="GrossAmount">Bildirilen brüt satış bedeli.</param>
/// <param name="MarketFeeAmount">Bildirilen hal rüsumu tutarı (BK-5, belediyeye ayrı raporlanır).</param>
/// <param name="OccurredOnUtc">Olayın gerçekleştiği an (UTC).</param>
public sealed record HksNotified(
    Guid HksNotificationId,
    Guid TenantId,
    Guid SaleTransactionId,
    string ReferenceNumber,
    decimal GrossAmount,
    decimal MarketFeeAmount,
    DateTime OccurredOnUtc) : IDomainEvent;

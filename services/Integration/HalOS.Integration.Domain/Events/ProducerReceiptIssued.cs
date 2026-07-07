using HalOS.BuildingBlocks.Domain;

namespace HalOS.Integration.Domain.Events;

/// <summary>
/// Bir e-Müstahsil Makbuzu (e-MM) başarıyla kesildiğinde yayınlanır (docs/02 §3.5 <c>DocumentIssued</c>;
/// docs/03 M7 / BK-4). Satış servisi (belge durumu), Bildirim (patrona/müşteriye haber) ve AI bunu
/// dinleyebilir (docs/02 §6). İç kullanım domain event'idir (Integration.Domain.Events); belge
/// kesilince el-yapımı outbox'a atomik yazılır ve OutboxDispatcher ile yayınlanır (docs/04 §10);
/// handler/consumer doğrudan yayın yapmaz (docs/07 §5). Event adı PascalCase geçmiş zaman (docs/07 §3).
/// </summary>
/// <param name="ProducerReceiptId">Kesilen e-MM belgesinin kimliği.</param>
/// <param name="TenantId">Belgenin ait olduğu işletme (tenant) — outbox tenant izolasyonu (BK-8).</param>
/// <param name="SaleTransactionId">e-MM'in kaynağı satış (idempotency anahtarı).</param>
/// <param name="ProducerPartyId">Müstahsil Party kimliği (belge bu tarafa düzenlenir).</param>
/// <param name="ReceiptNumber">Atanan makbuz numarası.</param>
/// <param name="NetPayable">Müstahsile ödenecek net tutar (brüt − stopaj − Bağ-Kur).</param>
/// <param name="OccurredOnUtc">Olayın gerçekleştiği an (UTC).</param>
public sealed record ProducerReceiptIssued(
    Guid ProducerReceiptId,
    Guid TenantId,
    Guid SaleTransactionId,
    Guid ProducerPartyId,
    string ReceiptNumber,
    decimal NetPayable,
    DateTime OccurredOnUtc) : IDomainEvent;

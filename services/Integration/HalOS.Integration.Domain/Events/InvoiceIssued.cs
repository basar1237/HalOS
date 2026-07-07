using HalOS.BuildingBlocks.Domain;

namespace HalOS.Integration.Domain.Events;

/// <summary>
/// Bir e-Fatura (HAL / <c>Invoice</c>) başarıyla kesildiğinde yayınlanır (docs/02 §3.5 / §6
/// <c>InvoiceIssued</c>; docs/03 M8 / BK-4). Satış servisi (belge durumu → satış "beklemede"den çıkar),
/// Bildirim (patrona/alıcıya haber) ve AI bunu dinleyebilir (docs/02 §6). İç kullanım domain event'idir
/// (Integration.Domain.Events); fatura kesilince el-yapımı outbox'a atomik yazılır ve OutboxDispatcher
/// ile yayınlanır (docs/04 §10); handler/consumer doğrudan yayın yapmaz (docs/07 §5). Event adı
/// PascalCase geçmiş zaman (docs/07 §3). <see cref="ProducerReceiptIssued"/> deseniyle birebir.
/// </summary>
/// <param name="InvoiceId">Kesilen e-Fatura belgesinin kimliği.</param>
/// <param name="TenantId">Belgenin ait olduğu işletme (tenant) — outbox tenant izolasyonu (BK-8).</param>
/// <param name="SaleTransactionId">e-Faturanın kaynağı satış (idempotency anahtarı).</param>
/// <param name="BuyerPartyId">Alıcı Party kimliği (fatura bu tarafa kesilir — docs/02 §1.2).</param>
/// <param name="InvoiceNumber">Atanan fatura numarası.</param>
/// <param name="TotalAmount">Fatura toplam tutarı (komisyon + komisyon KDV'si).</param>
/// <param name="OccurredOnUtc">Olayın gerçekleştiği an (UTC).</param>
public sealed record InvoiceIssued(
    Guid InvoiceId,
    Guid TenantId,
    Guid SaleTransactionId,
    Guid BuyerPartyId,
    string InvoiceNumber,
    decimal TotalAmount,
    DateTime OccurredOnUtc) : IDomainEvent;

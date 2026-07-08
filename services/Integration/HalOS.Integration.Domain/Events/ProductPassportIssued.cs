using HalOS.BuildingBlocks.Domain;

namespace HalOS.Integration.Domain.Events;

/// <summary>
/// Bir kalem için künye (<c>ProductPassport</c>) başarıyla üretilip HKS 19-haneli kod atandığında
/// yayınlanır (docs/02 §3.5 <c>ProductPassport</c>; docs/02 §6 event katalog satır 229:
/// ConsignmentReceived → e-Belge/künye; docs/03 M8 / BK-4). Stok, Bildirim ve AI bunu dinleyebilir
/// (docs/02 §6). İç kullanım domain event'idir (Integration.Domain.Events); künye üretilince el-yapımı
/// outbox'a atomik yazılır ve OutboxDispatcher ile yayınlanır (docs/04 §10); handler/consumer doğrudan
/// yayın yapmaz (docs/07 §5). Event adı PascalCase geçmiş zaman (docs/07 §3).
/// <see cref="ProducerReceiptIssued"/> / <see cref="HksNotified"/> deseniyle birebir.
/// </summary>
/// <param name="ProductPassportId">Üretilen künyenin kimliği.</param>
/// <param name="TenantId">Künyenin ait olduğu işletme (tenant) — outbox tenant izolasyonu (BK-8).</param>
/// <param name="ConsignmentId">Künyenin kaynağı mal geliş partisi (FK değil, docs/05 §5).</param>
/// <param name="ConsignmentItemId">Künyenin kaynağı parti kalemi (idempotency anahtarı — kalem başına tek künye).</param>
/// <param name="ProductId">Künyenin ait olduğu ürün referansı (FK değil, docs/05 §5).</param>
/// <param name="PassportCode">Atanan HKS 19-haneli künye kodu (QR ile sorgulanır).</param>
/// <param name="Quantity">Künyeye konu miktar (NUMERIC(18,3)).</param>
/// <param name="OccurredOnUtc">Olayın gerçekleştiği an (UTC).</param>
public sealed record ProductPassportIssued(
    Guid ProductPassportId,
    Guid TenantId,
    Guid ConsignmentId,
    Guid ConsignmentItemId,
    Guid ProductId,
    string PassportCode,
    decimal Quantity,
    DateTime OccurredOnUtc) : IDomainEvent;

using HalOS.BuildingBlocks.Domain;

namespace HalOS.BuildingBlocks.Contracts;

/// <summary>
/// Bir mal geliş partisi kabul edildiğinde yayınlanır (docs/02 §6 event katalog satır 229:
/// ConsignmentReceived → Stok, e-Belge/künye). Çekirdek servisler-arası entegrasyon event'i
/// olduğundan paylaşılan <c>Contracts</c> projesinde yaşar: tüketen servisler (Inventory stok
/// girişi, Integration künye = <c>ProductPassport</c>) tekrar sorgu yapmadan davranabilsin diye
/// gelen kalemleri (ürün/miktar/birim) mesajın kendisi taşır (docs/04 §10 event-taşımalı
/// entegrasyon; docs/07 §5 consumer içinde dış sorgu/hesap yok). Event adı PascalCase geçmiş
/// zaman (docs/07 §3).
///
/// <see cref="ITenantScopedEvent"/>'i uygular: broker üzerinden geçerken tenant bağlamı mesajın
/// kendisiyle taşınır, consumer <see cref="TenantId"/>'yi ambient tenant'a set eder (docs/07 §6 / BK-8).
///
/// <para>
/// Künye (<c>ProductPassport</c>, docs/02 §3.5) ürün-bazlıdır (tür/miktar/üretici) — HKS 19-haneli
/// kod üretim yeri, tür, miktar, üretici ve sertifika bilgisini içerir. Bu yüzden event tekil bir
/// parti özeti değil, <see cref="Items"/> kalem listesi taşır; Integration servisi her kalem için
/// künye kaydını yeniden sorgu yapmadan kurar (docs/03 M8, ADR-007). Kalem birimi
/// <see cref="ConsignmentReceivedItem.UnitCode"/> STRING'tir: Contracts assembly'si
/// <c>Sales.Domain</c>'e bağlanamaz, bu yüzden <c>UnitOfMeasure</c> enum'ı yerine unit kodu
/// (enum.ToString()) taşınır.
/// </para>
/// </summary>
/// <param name="ConsignmentId">Mal geliş partisinin kimliği.</param>
/// <param name="TenantId">Partinin bağlı olduğu işletme (tenant) — ITenantScopedEvent (BK-8).</param>
/// <param name="ProducerPartyId">Malı gönderen müstahsil/tüccar Party kimliği (FK değil, docs/05 §5).</param>
/// <param name="ReceivedAt">Malın fiilen kabul edildiği an.</param>
/// <param name="Items">Gelen kalemler (ürün/miktar/birim) — künye ürün-bazlı olduğundan taşınır (docs/02 §3.5).</param>
/// <param name="OccurredOnUtc">Olayın gerçekleştiği an (UTC) — sıra-dışı teslimatta monoton uygulama için.</param>
public sealed record ConsignmentReceived(
    Guid ConsignmentId,
    Guid TenantId,
    Guid ProducerPartyId,
    DateTime ReceivedAt,
    IReadOnlyList<ConsignmentReceivedItem> Items,
    DateTime OccurredOnUtc) : IDomainEvent, ITenantScopedEvent;

/// <summary>
/// <see cref="ConsignmentReceived"/> event'inin tek gelen kalemi (docs/02 §3.2; docs/05 §3.4
/// <c>consignment_item</c>). Consumer'lar (stok girişi, künye) ürün-bazlı davranabilsin diye
/// ürün/miktar/birim taşır. Ürün referansı ID ile (servisler arası FK yok — docs/05 §5).
/// </summary>
/// <param name="ConsignmentItemId">Kalem entity kimliği.</param>
/// <param name="ProductId">Ürün referansı (Inventory servisi ID'si — FK değil, docs/05 §5).</param>
/// <param name="Quantity">Gelen miktar (NUMERIC(18,3)).</param>
/// <param name="UnitCode">Birim kodu — Sales <c>UnitOfMeasure.ToString()</c> ile doldurur; Contracts
/// Sales.Domain'e bağlanamadığından enum yerine STRING taşınır (görev kısıtı).</param>
public sealed record ConsignmentReceivedItem(
    Guid ConsignmentItemId,
    Guid ProductId,
    decimal Quantity,
    string UnitCode);

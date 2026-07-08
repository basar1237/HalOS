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
///
/// <para>
/// Kesinti kırılımı (<see cref="AgriWithholdingAmount"/> + <see cref="FarmerSskAmount"/>) ayrıca
/// taşınır: e-Müstahsil Makbuzu (e-MM) YALNIZ stopaj + çiftçi Bağ-Kur içerir (komisyon/rüsum/KDV
/// e-MM'e GİRMEZ — docs/02 §1.3, BK-1/BK-4). <see cref="TotalDeductions"/> ise hakedişten düşülen
/// TÜM kalemleri (komisyon + stopaj + bağkur + rüsum) kapsar; dolayısıyla e-MM'i doğru kurmak için
/// yeterli DEĞİLDİR. Bu iki alan Sales'in zaten hesapladığı (<c>SettlementCalculation</c>) tutarlardır;
/// event'le taşınarak Integration servisi e-MM'i YENİDEN HESAPLAMADAN, tekil sorgu yapmadan kurar
/// (docs/04 §10 event-taşımalı entegrasyon; docs/07 §5 consumer içinde iş kararı, dış hesap yok).
/// </para>
/// </summary>
/// <param name="AgriWithholdingAmount">Zirai stopaj kesinti tutarı (docs/02 §1.3) — e-MM'e girer.</param>
/// <param name="FarmerSskAmount">Çiftçi Bağ-Kur (SGK) primi kesinti tutarı (docs/02 §1.3) — e-MM'e girer.</param>
/// <param name="MarketFeeAmount">Hal rüsumu kesinti tutarı = Round(gross × marketFeeRate, 2) (docs/02 §1.3
/// / BK-5). Belediyeye ödenen pazar rüsumudur (hal içi %1, hal dışı %2). Komisyon ve rüsum yasal olarak
/// AYRI saklanır — tek "fee" altında birleştirilmez (docs/02 §7 anti-pattern). <see cref="TotalDeductions"/>
/// rüsumu İÇERİR ama ayrıştırılamaz; HKS bildirimi ve <c>MarketFeeRecord</c> (BK-5, belediyeye 5 iş günü)
/// rüsum tutarını AYRI gerektirdiğinden ve consumer içinde yeniden hesap YASAK olduğundan (docs/07 §5)
/// ayrı taşınır. Sales'in zaten hesapladığı (<c>SettlementCalculation.MarketFee</c>) tutardır; event'le
/// taşınarak Integration servisi HKS/rüsum belgelerini yeniden hesaplamadan kurar (docs/04 §10).</param>
/// <param name="CommissionVatAmount">Komisyon üzerine hesaplanan KDV tutarı = Round(komisyon × KDV oranı, 2)
/// (docs/02 §4 / BK-1). Komisyoncunun geliridir; hakedişten DÜŞÜLMEZ (bu yüzden
/// <see cref="TotalDeductions"/>'a dahil değildir) ve e-MM'e GİRMEZ — yalnız e-Fatura tarafında
/// (Integration servisi Invoice akışı) kullanılır. Sales'in zaten hesapladığı
/// (<c>SettlementCalculation.VatOnCommission</c>) tutardır; event'le taşınarak alıcıya kesilecek
/// komisyon e-Faturasının KDV'si yeniden hesaplanmadan kurulur (docs/04 §10).</param>
/// <param name="Lines">Satış satırları (ürün + miktar kırılımı). Inventory servisi
/// (Stok &amp; Depo bağlamı — docs/02 §115) SaleCompleted'ı tüketip her satır için ilgili ürünün
/// stoğundan çıkış hareketi (StockMovement) yazar (docs/02 §6 SaleCompleted → Stok; §229-230 event
/// katalog). Stok çıkışı ürün + miktar gerektirdiğinden bu kırılım event'le taşınır; consumer
/// tekil sorgu yapmadan (docs/07 §5) stok düşer. Finans (cari) ve Integration (e-Belge/HKS)
/// consumer'ları bu alanı KULLANMAK ZORUNDA DEĞİLDİR — toplam/net tutarlarla çalışırlar.</param>
public sealed record SaleCompleted(
    Guid SaleTransactionId,
    Guid TenantId,
    Guid BuyerPartyId,
    Guid ProducerPartyId,
    DateTime SoldAt,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal CommissionVatAmount,
    decimal AgriWithholdingAmount,
    decimal FarmerSskAmount,
    decimal MarketFeeAmount,
    decimal TotalDeductions,
    decimal NetAmount,
    DateTime SettlementDueDate,
    IReadOnlyList<SaleCompletedLine> Lines,
    DateTime OccurredOnUtc) : IDomainEvent, ITenantScopedEvent;

/// <summary>
/// <see cref="SaleCompleted"/> event'inin tek bir satış satırını taşıyan alt kaydı (docs/02 §1.4
/// <c>SaleLine</c>; §6 SaleCompleted → Stok). Inventory servisi bu kırılımla ürün bazında stok
/// çıkışı (StockMovement) yazar — stok/bakiye = Σ hareket değişmezini korur (docs/02 §115).
///
/// Ölçü birimi <see cref="UnitCode"/> STRING olarak taşınır: Contracts assembly'si
/// <c>Sales.Domain</c>'e bağlanamaz (docs/07), bu yüzden <c>UnitOfMeasure</c> enum'ı event'te
/// enum yerine metin (<c>enum.ToString()</c>) olarak geçer.
/// </summary>
/// <param name="SaleLineId">Kaynak satış satırının kimliği (<c>SaleLine.Id</c>); consumer tarafında
/// stok hareketinin kaynak referansı/idempotency için kullanılabilir.</param>
/// <param name="ProductId">Ürün referansı (Inventory servisi ID'si — servisler arası FK yok, docs/05 §5).</param>
/// <param name="Quantity">Satılan miktar (NUMERIC(18,3); decimal — asla float, BK-2). Stok çıkış miktarıdır.</param>
/// <param name="UnitCode">Ölçü birimi kodu (<c>UnitOfMeasure.ToString()</c>) — enum değil metin (docs/07).</param>
public sealed record SaleCompletedLine(
    Guid SaleLineId,
    Guid ProductId,
    decimal Quantity,
    string UnitCode);

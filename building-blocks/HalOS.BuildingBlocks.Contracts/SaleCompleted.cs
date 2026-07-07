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
public sealed record SaleCompleted(
    Guid SaleTransactionId,
    Guid TenantId,
    Guid BuyerPartyId,
    Guid ProducerPartyId,
    DateTime SoldAt,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal AgriWithholdingAmount,
    decimal FarmerSskAmount,
    decimal TotalDeductions,
    decimal NetAmount,
    DateTime SettlementDueDate,
    DateTime OccurredOnUtc) : IDomainEvent, ITenantScopedEvent;

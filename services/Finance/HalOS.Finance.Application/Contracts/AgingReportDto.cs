namespace HalOS.Finance.Application.Contracts;

/// <summary>
/// Cari yaşlandırma raporu okuma DTO'su (docs/03 M10 "cari yaşlandırma (okuma)"). Tenant'ın
/// müstahsil hakediş (Settlement) vadelerini, referans tarihe (<c>AsOfUtc</c>) göre gecikme yaşına
/// göre kovalara (bucket) böler ve her kova için tutar + cari sayısı toplar. Tutarlar decimal (BK-2).
///
/// Yaşlandırma tabanı: hakediş hareketinin (<see cref="Domain.Enums.EntryType.Settlement"/>) ödeme
/// vade tarihi (<c>AccountEntry.DueDate</c>; normal satış +15 iş günü — BK-3). Kovalar:
/// <list type="bullet">
///   <item><b>Güncel</b>: vadesi henüz gelmemiş (DueDate &gt;= AsOf).</item>
///   <item><b>0-15 gün</b>: 1-15 gün gecikmiş.</item>
///   <item><b>16-30 gün</b>: 16-30 gün gecikmiş.</item>
///   <item><b>31+ gün</b>: 31 gün ve üzeri gecikmiş.</item>
/// </list>
/// Yeni tablo/servis YOK — mevcut AccountEntry verisi üzerinden agregasyon (SALT-OKUMA CQRS).
///
/// MVP basitleştirmesi (docs/07 §5; belirsizlik notu): her hakediş, planlanan tutarıyla
/// (<c>Amount</c>) vadesine göre yaşlandırılır; sonradan yapılan ödeme/avans (Debit) hareketleri
/// belirli hakedişlere MAHSUP EDİLMEZ (kova bazında allocation MVP dışı). Böylece kova tutarları
/// müstahsile planlanan brüt hakediş vadesini yansıtır. Alıcı borçları (Sale) vade taşımadığından
/// (DueDate null) bu rapora dahil edilmez — mevcut AccountEntry.DueDate/Type ile tutarlı davranış.
/// </summary>
/// <param name="AsOfUtc">Yaşlandırmanın hesaplandığı referans tarih (UTC).</param>
/// <param name="Current">Vadesi henüz gelmemiş hakediş kovası.</param>
/// <param name="Days0To15">1-15 gün gecikmiş hakediş kovası.</param>
/// <param name="Days16To30">16-30 gün gecikmiş hakediş kovası.</param>
/// <param name="Days31Plus">31+ gün gecikmiş hakediş kovası.</param>
/// <param name="TotalAmount">Tüm kovaların toplam tutarı.</param>
/// <param name="TotalAccountCount">
/// Rapora katkı sağlayan (en az bir hakediş vadesi olan) benzersiz cari sayısı.
/// </param>
public sealed record CurrentAccountAgingReportDto(
    DateTime AsOfUtc,
    AgingBucketDto Current,
    AgingBucketDto Days0To15,
    AgingBucketDto Days16To30,
    AgingBucketDto Days31Plus,
    decimal TotalAmount,
    int TotalAccountCount);

/// <summary>
/// Yaşlandırma kovası satırı (docs/03 M10). Bir gecikme aralığındaki toplam hakediş tutarı ve o
/// aralığa katkı sağlayan benzersiz cari sayısı. Tutar decimal (BK-2).
/// </summary>
/// <param name="Amount">Kovadaki toplam hakediş tutarı.</param>
/// <param name="AccountCount">Kovaya katkı sağlayan benzersiz cari sayısı.</param>
public sealed record AgingBucketDto(
    decimal Amount,
    int AccountCount);

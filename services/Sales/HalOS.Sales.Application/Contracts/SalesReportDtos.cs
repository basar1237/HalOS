namespace HalOS.Sales.Application.Contracts;

/// <summary>
/// Satış özet raporu okuma DTO'su (docs/03 M10 "raporlar (okuma)"). Belirli bir tarih aralığındaki
/// TAMAMLANMIŞ (Status=Completed) satışların agregasyonu. Tutarlar decimal (BK-2). Yeni tablo YOK —
/// mevcut SaleTransaction/Settlement/CommissionCalculation/Deduction verisi üzerinden okunur.
/// </summary>
/// <param name="Count">Aralıktaki tamamlanmış satış adedi.</param>
/// <param name="TotalGross">Toplam brüt satış bedeli = Σ SaleTransaction.GrossAmount.</param>
/// <param name="TotalCommission">Toplam komisyon = Σ CommissionCalculation.CommissionAmount.</param>
/// <param name="TotalDeductions">
/// KDV hariç toplam kesinti = Σ (komisyon + zirai stopaj + çiftçi Bağ-Kur + hal rüsumu). Komisyon
/// KDV'si (DeductionType.Vat) hakedişten düşülmez, bu toplama DAHİL DEĞİLDİR (docs/02 §4, BK-1).
/// </param>
/// <param name="TotalNet">Toplam net hakediş = Σ Settlement.NetAmount.</param>
public sealed record SalesSummaryReportDto(
    long Count,
    decimal TotalGross,
    decimal TotalCommission,
    decimal TotalDeductions,
    decimal TotalNet);

/// <summary>
/// Komisyon geliri raporu okuma DTO'su (docs/03 M10). Komisyoncunun aralıktaki komisyon geliri:
/// komisyon tutarı + komisyon KDV'si (docs/02 §4 — KDV komisyoncunun hesaplanan KDV'sidir).
/// </summary>
/// <param name="TotalCommission">Toplam komisyon tutarı = Σ CommissionCalculation.CommissionAmount.</param>
/// <param name="TotalVat">Toplam komisyon KDV'si = Σ CommissionCalculation.VatAmount.</param>
/// <param name="TotalIncome">Toplam komisyon geliri = TotalCommission + TotalVat.</param>
/// <param name="Daily">Günlük kırılım (SoldAt tarihine göre artan sıralı).</param>
public sealed record CommissionIncomeReportDto(
    decimal TotalCommission,
    decimal TotalVat,
    decimal TotalIncome,
    IReadOnlyList<CommissionIncomeDailyDto> Daily);

/// <summary>Komisyon geliri günlük kırılım satırı (docs/03 M10).</summary>
/// <param name="Day">Gün (SoldAt'in tarih kısmı, UTC).</param>
/// <param name="Commission">O günün komisyon tutarı toplamı.</param>
/// <param name="Vat">O günün komisyon KDV'si toplamı.</param>
/// <param name="Income">O günün toplam komisyon geliri = Commission + Vat.</param>
public sealed record CommissionIncomeDailyDto(
    DateTime Day,
    decimal Commission,
    decimal Vat,
    decimal Income);

/// <summary>
/// Gün sonu özet raporu okuma DTO'su (docs/03 M10). Verilen günün (SoldAt tarihi) tamamlanmış
/// satış toplamları. Tutarlar decimal (BK-2).
/// </summary>
/// <param name="Day">Rapor günü (tarih kısmı, UTC).</param>
/// <param name="Count">O günün tamamlanmış satış adedi.</param>
/// <param name="Gross">O günün toplam brüt bedeli.</param>
/// <param name="Commission">O günün toplam komisyonu.</param>
/// <param name="Net">O günün toplam net hakedişi.</param>
public sealed record DailySummaryReportDto(
    DateTime Day,
    long Count,
    decimal Gross,
    decimal Commission,
    decimal Net);

/// <summary>
/// Kontrol paneli satış-tarafı özet DTO'su (docs/02 §5 günlük akış kartları). Dashboard'ın
/// "Bugünkü Mal Geliş" ve "Bekleyen Hakediş" kartlarını besler. SALT-OKUMA CQRS; tenant filtreli (BK-8).
/// </summary>
/// <param name="TodayConsignmentCount">Verilen günde (ReceivedAt tarihi) kabul edilen mal geliş partisi adedi.</param>
/// <param name="PendingSettlementTotal">
/// Ödenmemiş müstahsil hakedişi toplamı = Σ Settlement.NetAmount (tamamlanmış satış, Settlement.Status ≠ Paid).
/// </param>
public sealed record SalesDashboardDto(
    long TodayConsignmentCount,
    decimal PendingSettlementTotal);

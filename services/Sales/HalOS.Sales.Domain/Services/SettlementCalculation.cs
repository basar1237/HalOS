using HalOS.Sales.Domain.ValueObjects;

namespace HalOS.Sales.Domain.Services;

/// <summary>
/// <see cref="SettlementCalculator"/>'ın ürettiği saf hesap sonucu (docs/03 §4 BK-1/BK-2). Tüm
/// tutarlar decimal ve kuruşa yuvarlıdır (BK-2). Persistence'a bağlı DEĞİLDİR; SaleTransaction
/// bu sonuçtan CommissionCalculation + Deduction'lar + Settlement entity'lerini üretir.
/// </summary>
/// <param name="Gross">Brüt satış bedeli = Σ satır tutarı (BK-1).</param>
/// <param name="Commission">Komisyon = Round(gross × commissionRate, 2, ToEven).</param>
/// <param name="AgriWithholding">Zirai stopaj = Round(gross × agriWithholdingRate, 2).</param>
/// <param name="FarmerSsk">Çiftçi Bağ-Kur = Round(gross × farmerSskRate, 2).</param>
/// <param name="MarketFee">Hal rüsumu = Round(gross × marketFeeRate, 2).</param>
/// <param name="VatOnCommission">Komisyon KDV'si = Round(commission × vatRate, 2) — hakedişten DÜŞÜLMEZ (BK-1).</param>
/// <param name="Net">Müstahsile net = gross − (commission + agriWithholding + farmerSsk + marketFee). Negatif olamaz.</param>
/// <param name="Rates">Hesapta kullanılan oran kümesi (satış anında dondurulur).</param>
public readonly record struct SettlementCalculation(
    decimal Gross,
    decimal Commission,
    decimal AgriWithholding,
    decimal FarmerSsk,
    decimal MarketFee,
    decimal VatOnCommission,
    decimal Net,
    RateSet Rates)
{
    /// <summary>Hakedişten düşülen kesintiler toplamı (KDV HARİÇ — BK-1).</summary>
    public decimal TotalDeductions => Commission + AgriWithholding + FarmerSsk + MarketFee;
}

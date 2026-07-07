using HalOS.Sales.Domain.ValueObjects;

namespace HalOS.Sales.Domain.Services;

/// <summary>
/// Kesinti/hakediş motoru — SİSTEMİN KALBİ (docs/02 §4, docs/03 §4 BK-1/BK-2). Brüt satış
/// bedeli ve satış anında dondurulmuş <see cref="RateSet"/> üzerinden komisyon, zirai stopaj,
/// çiftçi Bağ-Kur, hal rüsumu, komisyon KDV'si ve müstahsile net hakedişi hesaplar. Saftır
/// (yan etkisiz, persistence'sız) → hızlı birim testli (docs/07 §4/§7).
///
/// Hesap kuralı (BK-1/BK-2):
/// - Her kesinti brüt üzerinden ve tek seferde yuvarlanır: <c>Round(gross × rate, 2, ToEven)</c>.
/// - Komisyon KDV'si komisyon üzerinden: <c>Round(commission × vatRate, 2)</c>; hakedişten DÜŞÜLMEZ.
/// - Net = gross − (commission + agriWithholding + farmerSsk + marketFee). ASLA negatif olmamalı;
///   olası negatif durum çağıran tarafta <c>Settlement.Create</c> ile reddedilir (değişmez).
/// - Yuvarlama YALNIZCA son adımda kuruşa, banker's rounding (<see cref="Money"/>).
/// </summary>
public static class SettlementCalculator
{
    /// <summary>
    /// Brüt bedel ve oran kümesinden kesinti/hakediş hesabını üretir (BK-1/BK-2). Oran doğrulaması
    /// (komisyon ≤ %8, negatif oran yok) <see cref="RateSet"/> oluşturulurken yapılır; buraya
    /// geçerli bir RateSet gelir.
    /// </summary>
    public static SettlementCalculation Calculate(decimal gross, RateSet rates)
    {
        var commission = Money.ApplyRate(gross, rates.CommissionRate);
        var agriWithholding = Money.ApplyRate(gross, rates.AgriWithholdingRate);
        var farmerSsk = Money.ApplyRate(gross, rates.FarmerSskRate);
        var marketFee = Money.ApplyRate(gross, rates.MarketFeeRate);

        // Komisyon KDV'si komisyon tutarı üzerinden — komisyoncu geliri; hakedişten düşülmez (BK-1).
        var vatOnCommission = Money.ApplyRate(commission, rates.VatRate);

        var totalDeductions = commission + agriWithholding + farmerSsk + marketFee;
        var net = Money.RoundToKurus(gross - totalDeductions);

        return new SettlementCalculation(
            gross,
            commission,
            agriWithholding,
            farmerSsk,
            marketFee,
            vatOnCommission,
            net,
            rates);
    }
}

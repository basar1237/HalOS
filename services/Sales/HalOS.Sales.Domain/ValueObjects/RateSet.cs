using HalOS.BuildingBlocks.Domain;

namespace HalOS.Sales.Domain.ValueObjects;

/// <summary>
/// Bir satışa uygulanacak kesinti oranları kümesi (docs/02 §4, docs/03 §4 BK-1). Satış anında
/// tenant config + taraf stopaj profilinden çözülür (bkz. Application IRateProvider portu) ve
/// <c>SaleTransaction.Complete</c>'e verilir; böylece geçmiş satışlar kendi anındaki oranlarla
/// dondurulur (docs/02 §4 "tenant + tarih + taraf").
///
/// Oranlar <see cref="decimal"/> (asla float/double — docs/07 §4 / BK-2); NUMERIC(7,4) ölçeğine
/// karşılık gelir (örn. 0.0800 = %8). Değişmezler:
/// - <see cref="CommissionRate"/> ≤ %8 (0.08) ZORUNLU; aşarsa RateSet geçersizdir (BK-1).
/// - Tüm oranlar ≥ 0.
/// - <see cref="MarketFeeRate"/> hal içi %1 / hal dışı %2 (BK-5) — çağıran <c>isWithinMarket</c>'e
///   göre <see cref="ForMarket"/> ile üretir.
/// Yapısal eşitliğe sahiptir.
/// </summary>
public sealed class RateSet : ValueObject
{
    /// <summary>Komisyon oranı üst sınırı (docs/02 §1.3, docs/03 §4 BK-1: maks %8).</summary>
    public const decimal MaxCommissionRate = 0.08m;

    /// <summary>Hal içi satış rüsum oranı (%1) — docs/03 §4 BK-5.</summary>
    public const decimal WithinMarketFeeRate = 0.01m;

    /// <summary>Hal dışı satış rüsum oranı (%2) — docs/03 §4 BK-5.</summary>
    public const decimal OutsideMarketFeeRate = 0.02m;

    private RateSet(
        decimal commissionRate,
        decimal agriWithholdingRate,
        decimal farmerSskRate,
        decimal marketFeeRate,
        decimal vatRate)
    {
        CommissionRate = commissionRate;
        AgriWithholdingRate = agriWithholdingRate;
        FarmerSskRate = farmerSskRate;
        MarketFeeRate = marketFeeRate;
        VatRate = vatRate;
    }

    /// <summary>Komisyon oranı (docs/02 §1.3 <c>Commission</c>, maks %8).</summary>
    public decimal CommissionRate { get; }

    /// <summary>Zirai stopaj oranı (docs/02 §1.3 <c>AgriculturalWithholding</c>, tipik %2).</summary>
    public decimal AgriWithholdingRate { get; }

    /// <summary>Çiftçi Bağ-Kur (SGK) primi oranı (docs/02 §1.3 <c>FarmerSocialSecurity</c>, tipik %1).</summary>
    public decimal FarmerSskRate { get; }

    /// <summary>Hal rüsumu oranı (docs/02 §1.3 <c>MarketFee</c>: hal içi %1 / hal dışı %2, BK-5).</summary>
    public decimal MarketFeeRate { get; }

    /// <summary>Komisyon KDV oranı (docs/02 §1.3 <c>Vat</c>). Komisyon üzerine uygulanır; müstahsil
    /// hakedişinden düşülmez (BK-1).</summary>
    public decimal VatRate { get; }

    /// <summary>
    /// Verilen oranlarla bir RateSet oluşturur. <paramref name="isWithinMarket"/> rüsum oranını
    /// belirler (hal içi %1 / hal dışı %2 — BK-5). Komisyon %8'i aşarsa veya herhangi bir oran
    /// negatifse başarısız döner (BK-1).
    /// </summary>
    public static Result<RateSet> Create(
        decimal commissionRate,
        decimal agriWithholdingRate,
        decimal farmerSskRate,
        bool isWithinMarket,
        decimal vatRate)
    {
        var marketFeeRate = isWithinMarket ? WithinMarketFeeRate : OutsideMarketFeeRate;

        if (commissionRate < 0m ||
            agriWithholdingRate < 0m ||
            farmerSskRate < 0m ||
            vatRate < 0m)
        {
            return Result.Failure<RateSet>(RateSetErrors.NegativeRate);
        }

        // Komisyon %8'i AŞAMAZ (docs/03 §4 BK-1; docs/07 §4). Değişmez, testle korunur.
        if (commissionRate > MaxCommissionRate)
        {
            return Result.Failure<RateSet>(RateSetErrors.CommissionRateTooHigh);
        }

        return new RateSet(commissionRate, agriWithholdingRate, farmerSskRate, marketFeeRate, vatRate);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CommissionRate;
        yield return AgriWithholdingRate;
        yield return FarmerSskRate;
        yield return MarketFeeRate;
        yield return VatRate;
    }
}

public static class RateSetErrors
{
    public static readonly Error CommissionRateTooHigh =
        new("RateSet.CommissionRateTooHigh", "Komisyon oranı %8'i (0,08) aşamaz.");

    public static readonly Error NegativeRate =
        new("RateSet.NegativeRate", "Kesinti oranları negatif olamaz.");
}

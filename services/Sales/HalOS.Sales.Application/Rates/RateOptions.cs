namespace HalOS.Sales.Application.Rates;

/// <summary>
/// Config-tabanlı varsayılan kesinti oranları (docs/02 §4; docs/07 §10: sihirli sabit yerine
/// adlandırılmış config). <see cref="DefaultRateProvider"/> tarafından okunur. Değerler oran
/// (decimal, NUMERIC(7,4) ölçeği; örn. 0.08 = %8). Rüsum oranı satışın hal içi/dışı olmasına
/// göre RateSet içinde belirlenir (BK-5), bu yüzden burada tutulmaz.
/// </summary>
public sealed class RateOptions
{
    public const string SectionName = "Rates";

    /// <summary>Varsayılan komisyon oranı (docs/02 §1.3, maks %8 — RateSet doğrular).</summary>
    public decimal DefaultCommissionRate { get; set; } = 0.08m;

    /// <summary>Varsayılan zirai stopaj oranı (docs/02 §1.3, tipik %2).</summary>
    public decimal AgriWithholdingRate { get; set; } = 0.02m;

    /// <summary>Varsayılan çiftçi Bağ-Kur oranı (docs/02 §1.3, tipik %1).</summary>
    public decimal FarmerSskRate { get; set; } = 0.01m;

    /// <summary>Komisyon KDV oranı (docs/02 §1.3; komisyoncu geliri, tipik %20).</summary>
    public decimal CommissionVatRate { get; set; } = 0.20m;
}

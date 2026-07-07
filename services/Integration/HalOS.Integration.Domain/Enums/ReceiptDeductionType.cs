namespace HalOS.Integration.Domain.Enums;

/// <summary>
/// e-Müstahsil Makbuzu (e-MM) kesinti kalemi türü (docs/02 §1.3 / §3.5). e-MM bir MÜSTAHSİL-ALIM
/// belgesidir ve YALNIZ müstahsilden kesilen stopaj + çiftçi Bağ-Kur kalemlerini içerir; komisyon,
/// hal rüsumu ve komisyon KDV'si e-MM'e GİRMEZ (bunlar komisyoncu-alıcı ilişkisine ait; docs/02
/// §1.2/§1.3, BK-1/BK-4). Enum kolonu metin (HasConversion&lt;string&gt; — docs/07).
/// </summary>
public enum ReceiptDeductionType
{
    /// <summary>Zirai stopaj — müstahsilden gelir vergisi kesintisi (docs/02 §1.3, tipik %2).</summary>
    AgriWithholding = 1,

    /// <summary>Çiftçi Bağ-Kur (SGK) primi — müstahsilden kesilir (docs/02 §1.3, tipik %1).</summary>
    FarmerSsk = 2
}

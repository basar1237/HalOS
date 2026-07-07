namespace HalOS.Sales.Domain.Enums;

/// <summary>
/// Kesinti kalemi türü (docs/05 §3.5 <c>deduction.type</c>). Komisyon ve rüsum yasal olarak
/// AYRI saklanır — tek "fee" altında birleştirilmez (docs/02 §7 anti-pattern). KDV (vat)
/// komisyoncunun HESAPLANAN KDV'sidir; müstahsil hakedişinden DÜŞÜLMEZ (docs/02 §4, BK-1).
/// </summary>
public enum DeductionType
{
    /// <summary>Komisyon — komisyoncu ücreti (docs/02 §1.3, maks %8). Hakedişten düşülür.</summary>
    Commission = 1,

    /// <summary>Zirai stopaj — müstahsilden gelir vergisi kesintisi (tipik %2). Hakedişten düşülür.</summary>
    AgriWithholding = 2,

    /// <summary>Çiftçi Bağ-Kur (SGK) primi — müstahsilden kesilir (tipik %1). Hakedişten düşülür.</summary>
    FarmerSsk = 3,

    /// <summary>Hal rüsumu — belediye pazar rüsumu (hal içi %1 / hal dışı %2). Hakedişten düşülür.</summary>
    MarketFee = 4,

    /// <summary>Komisyon KDV'si — komisyoncunun hesaplanan KDV'si. Hakedişten DÜŞÜLMEZ (BK-1).</summary>
    Vat = 5
}

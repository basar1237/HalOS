namespace HalOS.Integration.Domain.Enums;

/// <summary>
/// e-Fatura senaryosu (docs/02 §1.2). Halde kesilen faturalar GİB'e <c>HAL</c> senaryosu ile
/// gönderilir. Enum kolonu metin (HasConversion&lt;string&gt; — docs/07). Bu slice'ta yalnız
/// <see cref="Hal"/> kullanılır (kavram sabitlenir, genişlemeye açık).
/// </summary>
public enum InvoiceScenario
{
    /// <summary>Hal senaryosu — sebze-meyve hali komisyon/satış faturası (docs/02 §1.2).</summary>
    Hal = 1
}

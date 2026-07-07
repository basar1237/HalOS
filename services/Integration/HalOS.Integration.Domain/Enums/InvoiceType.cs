namespace HalOS.Integration.Domain.Enums;

/// <summary>
/// e-Fatura (HAL) türü (docs/02 §1.2): <c>KOMİSYON</c> (komisyoncunun aracılık komisyonu faturası) veya
/// <c>SATIŞ</c> (tüccarın kendi malını sattığı fatura). Enum kolonu metin (HasConversion&lt;string&gt;
/// — docs/07). Bu slice'ta komisyoncu senaryosunda <see cref="Commission"/> üretilir (alıcıya kesilen
/// komisyon + komisyon KDV'si — SaleCompleted taşır, yeniden hesap yok).
/// </summary>
public enum InvoiceType
{
    /// <summary>KOMİSYON — komisyoncunun alıcıya kestiği aracılık komisyonu faturası (docs/02 §1.2).</summary>
    Commission = 1,

    /// <summary>SATIŞ — tüccarın kendi malını sattığı fatura (docs/02 §1.2).</summary>
    Sale = 2
}

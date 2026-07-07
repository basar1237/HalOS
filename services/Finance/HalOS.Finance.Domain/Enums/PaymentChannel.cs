namespace HalOS.Finance.Domain.Enums;

/// <summary>
/// Ödeme/tahsilat kanalı (docs/05 §3.7 <c>payment/collection/advance.channel</c>: cash/bank).
/// BK-6: 7.000 TL üstü nakit yasaktır; bu eşiği aşan tutarlar banka üzerinden ve belgeli
/// olmalıdır. Enum kolonu metin (HasConversion&lt;string&gt; — docs/07).
/// </summary>
public enum PaymentChannel
{
    /// <summary>Nakit — 7.000 TL'yi aşamaz (BK-6).</summary>
    Cash = 1,

    /// <summary>Banka/finansal kuruluş — belgeli; büyük tutarlar için zorunlu (BK-6).</summary>
    Bank = 2
}

namespace HalOS.Integration.Domain.Enums;

/// <summary>
/// e-Fatura (HAL / <c>Invoice</c>) yaşam döngüsü durumu (docs/02 §3.5 <c>LegalDocument</c> alt tipi;
/// docs/03 M8 / BK-4). Enum kolonu metin (HasConversion&lt;string&gt; — docs/07). GİB'e gönderim
/// ADR-007 gereği retry + outbox ile yapılır; bu slice'ta gönderim STUB'tur (gerçek GİB e-Fatura
/// sandbox entegrasyonu sonraki slice). e-MM (<c>ProducerReceiptStatus</c>) deseniyle birebir.
/// </summary>
public enum InvoiceStatus
{
    /// <summary>Fatura oluşturuldu ama henüz GİB'e gönderilmedi (taslak).</summary>
    Draft = 1,

    /// <summary>Fatura başarıyla kesildi/gönderildi; fatura numarası atandı.</summary>
    Issued = 2,

    /// <summary>Gönderim/red nedeniyle başarısız — kullanıcı uyarılır, yeniden denenebilir (docs/03 BK-4).</summary>
    Failed = 3,

    /// <summary>Fatura iptal edildi (satış iptali/ters kayıt; denetim izi korunur — BK-9).</summary>
    Cancelled = 4
}

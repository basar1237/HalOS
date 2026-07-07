namespace HalOS.Finance.Domain.Enums;

/// <summary>
/// Cari hareket türü (docs/05 §3.7 <c>account_entry.entry_type</c>: sale / settlement / payment /
/// collection / advance / adjustment). Hareketin hangi iş olayından doğduğunu belirtir; bakiye
/// yönü <see cref="EntryDirection"/> ile ayrı taşınır. Enum kolonu metin (HasConversion&lt;string&gt;).
/// </summary>
public enum EntryType
{
    /// <summary>Satış kaynaklı borç kaydı — alıcının ödeyeceği brüt tutar (SaleCompleted).</summary>
    Sale = 1,

    /// <summary>Hakediş kaynaklı alacak kaydı — müstahsile net hakediş (SaleCompleted).</summary>
    Settlement = 2,

    /// <summary>Müstahsile yapılan ödeme (alacağı azaltır — borç kaydı).</summary>
    Payment = 3,

    /// <summary>Alıcıdan yapılan tahsilat (borcunu azaltır — alacak kaydı).</summary>
    Collection = 4,

    /// <summary>Avans — teslimat/satış öncesi peşin ödeme; ileride mahsuplaşır (docs/02 §3.4).</summary>
    Advance = 5,

    /// <summary>Düzeltme/mahsup kaydı (ters kayıt; append-only defterde denetim izi korunur).</summary>
    Adjustment = 6
}

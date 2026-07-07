namespace HalOS.Finance.Domain.Enums;

/// <summary>
/// Cari hareket yönü (docs/05 §3.7 <c>account_entry.direction</c>). Bakiye türetilirken borç
/// pozitif, alacak negatif etkir (bkz. <see cref="Aggregates.CurrentAccount"/> değişmez notu):
/// - Alıcının cariye BORCU (alıcı öder) → <see cref="Debit"/>.
/// - Müstahsile ALACAK (komisyoncu öder) / avans → <see cref="Credit"/>.
/// Enum kolonu metin olarak saklanır (HasConversion&lt;string&gt; — docs/07).
/// </summary>
public enum EntryDirection
{
    /// <summary>Borç — tarafın işletmeye borcunu artıran hareket (ör. alıcı satış borcu).</summary>
    Debit = 1,

    /// <summary>Alacak — tarafın işletmeden alacağını artıran hareket (ör. müstahsil hakedişi).</summary>
    Credit = 2
}

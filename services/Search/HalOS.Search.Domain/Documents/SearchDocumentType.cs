namespace HalOS.Search.Domain.Documents;

/// <summary>
/// Aranabilir doküman türleri (docs/06 S2.3). Türe göre filtre (<c>/search?type=</c>) ve indeks
/// eşlemesi için sabit anahtarlar. Kod adı İngilizce/PascalCase (docs/07 §3), değer olarak da bu
/// metin kullanılır (ES <c>type</c> alanı + InMemory eşleşme). Belge/e-fatura türleri FOLLOW-UP
/// (ilgili event'ler Contracts'ta değil).
/// </summary>
public static class SearchDocumentType
{
    /// <summary>Taraf (cari kart) dokümanı — PartyRegistered'dan indekslenir.</summary>
    public const string Party = "Party";

    /// <summary>Satış dokümanı — SaleCompleted'dan indekslenir.</summary>
    public const string Sale = "Sale";

    /// <summary>
    /// Serbest istemci girdisini (ör. <c>/search?type=party</c>) bilinen bir tür sabitine
    /// case-INSENSITIVE eşleyerek KANONİK değere (<see cref="Party"/>/<see cref="Sale"/>) çevirir.
    /// Bu tek-nokta normalizasyon, aynı girdinin InMemory ve Elasticsearch backend'lerinde AYNI
    /// sonucu vermesini garanti eder: ES <c>type</c> keyword alanına birebir (case-sensitive) term
    /// filter uygular, dolayısıyla index katmanına daima kanonik değer geçmelidir.
    /// </summary>
    /// <param name="input">Ham tür girdisi; null/boş ise filtre yok kabul edilir.</param>
    /// <param name="canonical">Eşleşen kanonik tür sabiti; null/boş girdide de null döner.</param>
    /// <returns>
    /// <c>true</c>: girdi null/boş (filtre yok, <paramref name="canonical"/>=null) YA DA bilinen bir
    /// türe eşleşti (<paramref name="canonical"/>=kanonik değer). <c>false</c>: girdi dolu ama bilinen
    /// bir türe eşleşmedi (çağıran 400 döndürmeli).
    /// </returns>
    public static bool TryNormalize(string? input, out string? canonical)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            canonical = null;
            return true;
        }

        var trimmed = input.Trim();

        if (string.Equals(trimmed, Party, StringComparison.OrdinalIgnoreCase))
        {
            canonical = Party;
            return true;
        }

        if (string.Equals(trimmed, Sale, StringComparison.OrdinalIgnoreCase))
        {
            canonical = Sale;
            return true;
        }

        canonical = null;
        return false;
    }
}

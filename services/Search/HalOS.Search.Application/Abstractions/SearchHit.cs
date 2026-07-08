namespace HalOS.Search.Application.Abstractions;

/// <summary>
/// Bir arama sonucu satırı (docs/06 S2.3). Arama motorundan (ES ya da InMemory) dönen dokümanın
/// kullanıcıya taşınan alt kümesi: kimlik, tür ve gösterim özeti. Ham doküman değil kasıtlı bir
/// projeksiyon — API sözleşmesi arama deposundan bağımsız kalır.
/// </summary>
/// <param name="Id">Doküman kimliği (tür-önekli, ör. "Party:...").</param>
/// <param name="Type">Doküman türü (<c>SearchDocumentType</c>).</param>
/// <param name="Summary">Kullanıcıya gösterilecek kısa özet.</param>
public sealed record SearchHit(string Id, string Type, string Summary);

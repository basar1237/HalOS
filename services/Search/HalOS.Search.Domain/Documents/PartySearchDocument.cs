namespace HalOS.Search.Domain.Documents;

/// <summary>
/// Taraf (cari kart) arama dokümanı (docs/06 S2.3 — "Ali'nin her şeyini 1 sn'de"). Party servisinin
/// <c>PartyRegistered</c> event'inden indekslenir; Party DB'sine DOKUNULMAZ (CQRS ayrı okuma modeli,
/// docs/04 ADR-007). Görünen ad, kimlik numarası (TCKN/VKN) ve rol(ler) üzerinden aranabilir.
/// </summary>
public sealed class PartySearchDocument : SearchDocument
{
    /// <summary>Kaynak <c>Party.Id</c> (GUID metni). <see cref="MakeId"/> ile doküman Id'sine eşlenir.</summary>
    public required Guid PartyId { get; init; }

    /// <summary>Görünen ad (aranabilir; sonuç etiketi).</summary>
    public required string DisplayName { get; init; }

    /// <summary>Kimlik numarası (TCKN/VKN) — aranabilir; yoksa null.</summary>
    public string? TaxNumber { get; init; }

    /// <summary>Taraf rol(ler)i, virgülle ayrılmış metin (ör. "Producer,Buyer").</summary>
    public required string PartyType { get; init; }

    /// <inheritdoc />
    public override string SearchableText =>
        string.Join(" ", new[] { DisplayName, TaxNumber, PartyType }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>Kaynak taraf kimliğinden deterministik doküman Id üretir (idempotent upsert).</summary>
    public static string MakeId(Guid partyId) => $"{SearchDocumentType.Party}:{partyId:N}";
}

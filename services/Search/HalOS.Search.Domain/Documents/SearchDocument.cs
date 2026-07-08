namespace HalOS.Search.Domain.Documents;

/// <summary>
/// Aranabilir bir okuma-modeli dokümanının ORTAK sözleşmesi (docs/06 S2.3; docs/04 ADR-007). Tüm
/// arama dokümanları tek bir tenant-agnostik ES indeksinde yaşar; her doküman <see cref="TenantId"/>
/// taşır ve arama JWT tenant'ına GÖRE term filter ile daraltılır — çapraz-tenant sızıntısı YASAK
/// (BK-8). Türe göre filtre için <see cref="Type"/> (<see cref="SearchDocumentType"/>) taşınır.
///
/// Somut tipler (<see cref="PartySearchDocument"/>, <see cref="SaleSearchDocument"/>) indekslenen
/// alanları ve serbest-metin <see cref="Summary"/>'yi doldurur. <see cref="SearchableText"/> arama
/// motorunun (InMemory contains ya da ES <c>copy_to</c>/analiz) hedefidir.
/// </summary>
public abstract class SearchDocument
{
    /// <summary>Dokümanın tekil kimliği. Kaynak varlık ID'sinden türetilir (idempotent upsert).</summary>
    public required string Id { get; init; }

    /// <summary>Dokümanın ait olduğu kiracı (tenant) — arama term filter'ı bunu kullanır (BK-8).</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Doküman türü (<see cref="SearchDocumentType"/>). Tür filtresi ve indeks eşlemesi için.</summary>
    public required string Type { get; init; }

    /// <summary>Kullanıcıya gösterilecek kısa özet (arama sonuç satırı). Örn. taraf adı + kimlik no.</summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Serbest-metin arama hedefi: dokümanın aranabilir tüm alanlarının birleşimi. InMemory
    /// uygulaması burada case-insensitive contains eşleşmesi yapar; ES tarafında analiz edilen
    /// tam-metin alanına karşılık gelir. Somut tip bu değeri kurar.
    /// </summary>
    public abstract string SearchableText { get; }
}

namespace HalOS.Search.Application.Search;

/// <summary>
/// Arama sorgusu (docs/06 S2.3). Tenant sorgunun İÇİNDE taşınmaz — çağıran (API) JWT tenant
/// claim'inden çözüp <see cref="SearchQueryHandler"/>'a ayrı parametre olarak verir; böylece
/// tenant kapsamı istemci girdisinden değil kimlikten belirlenir (BK-8, çapraz-tenant sızıntısı YASAK).
/// </summary>
/// <param name="Query">Serbest-metin sorgu (ör. taraf adı ya da kimlik no).</param>
/// <param name="Type">
/// Opsiyonel tür filtresi; null ise tüm türler. KANONİK <c>SearchDocumentType</c> değeri beklenir —
/// çağıran (API) ham istemci girdisini <c>SearchDocumentType.TryNormalize</c> ile kanonikleştirir,
/// böylece InMemory ve ES backend'leri aynı değeri alıp aynı sonucu verir.
/// </param>
/// <param name="Size">Dönecek azami sonuç sayısı.</param>
public sealed record SearchQuery(string Query, string? Type, int Size);

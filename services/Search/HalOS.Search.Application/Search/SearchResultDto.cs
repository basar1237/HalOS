using HalOS.Search.Application.Abstractions;

namespace HalOS.Search.Application.Search;

/// <summary>
/// Arama yanıtı DTO'su (docs/06 S2.3). Toplam eşleşme sayısı yerine dönen sonuç listesini taşır
/// (basit MVP arama; sayfalama FOLLOW-UP). API sözleşmesini arama deposundan yalıtır.
/// </summary>
/// <param name="Hits">Bulunan sonuç satırları.</param>
public sealed record SearchResultDto(IReadOnlyList<SearchHit> Hits);

namespace HalOS.Party.Application.Contracts;

/// <summary>Basit sayfalama sonucu (docs/03 M1 — basit sayfalama).</summary>
/// <typeparam name="T">Öğe tipi.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);

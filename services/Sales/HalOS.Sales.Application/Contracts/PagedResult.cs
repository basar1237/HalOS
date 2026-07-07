namespace HalOS.Sales.Application.Contracts;

/// <summary>Basit sayfalama sonucu (docs/03 M4).</summary>
/// <typeparam name="T">Öğe tipi.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);

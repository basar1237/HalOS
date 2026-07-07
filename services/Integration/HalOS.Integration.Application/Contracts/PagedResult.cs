namespace HalOS.Integration.Application.Contracts;

/// <summary>Basit sayfalama sonucu (Finance/Sales.PagedResult deseniyle birebir).</summary>
/// <typeparam name="T">Öğe tipi.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);

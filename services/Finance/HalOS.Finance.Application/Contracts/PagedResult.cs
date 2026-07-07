namespace HalOS.Finance.Application.Contracts;

/// <summary>Basit sayfalama sonucu (Sales.PagedResult deseniyle birebir).</summary>
/// <typeparam name="T">Öğe tipi.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);

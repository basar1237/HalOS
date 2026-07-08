namespace HalOS.Inventory.Application.Contracts;

/// <summary>Basit sayfalama sonucu (Finance.PagedResult deseniyle birebir).</summary>
/// <typeparam name="T">Öğe tipi.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);

using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.ListProducts;

/// <summary>
/// Tenant'ın ürünlerini ada göre sıralı sayfalı listeler (docs/03 M2). Tenant global filter otomatik
/// (BK-8). <paramref name="OnlyActive"/> true ise yalnız aktif ürünler (seçiciler için).
/// </summary>
public sealed record ListProductsQuery(
    int Page = 1,
    int PageSize = 20,
    bool OnlyActive = true) : IQuery<PagedResult<ProductDto>>;

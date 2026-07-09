using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.ListProducts;

/// <summary>Tenant'ın ürünlerini sayfalı listeleyen query handler (docs/03 M2). Tenant filtreli (BK-8).</summary>
internal sealed class ListProductsHandler
    : IQueryHandler<ListProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _products;

    public ListProductsHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(
        ListProductsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _products.ListAsync(
            request.Page,
            request.PageSize,
            request.OnlyActive,
            cancellationToken);

        IReadOnlyList<ProductDto> dto = items.Select(ProductDto.FromDomain).ToList();
        return Result.Success(new PagedResult<ProductDto>(dto, request.Page, request.PageSize, total));
    }
}

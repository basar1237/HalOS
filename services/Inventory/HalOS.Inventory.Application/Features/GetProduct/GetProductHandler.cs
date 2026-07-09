using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Application.Contracts;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.GetProduct;

/// <summary>Tekil ürünü getiren query handler (docs/03 M2). Tenant filtreli (BK-8); yoksa NotFound.</summary>
internal sealed class GetProductHandler : IQueryHandler<GetProductQuery, ProductDto>
{
    private readonly IProductRepository _products;

    public GetProductHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Result<ProductDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.Id, cancellationToken);
        return product is null
            ? Result.Failure<ProductDto>(ProductErrors.NotFound)
            : Result.Success(ProductDto.FromDomain(product));
    }
}

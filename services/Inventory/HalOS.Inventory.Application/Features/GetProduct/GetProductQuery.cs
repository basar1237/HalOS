using HalOS.BuildingBlocks.Application;
using HalOS.Inventory.Application.Contracts;

namespace HalOS.Inventory.Application.Features.GetProduct;

/// <summary>Tekil ürün (tenant filtreli; docs/03 M2).</summary>
public sealed record GetProductQuery(Guid Id) : IQuery<ProductDto>;

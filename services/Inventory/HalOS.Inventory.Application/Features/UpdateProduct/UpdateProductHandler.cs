using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.UpdateProduct;

/// <summary>
/// Ürün güncelleyen handler (docs/03 M2). Tenant filtreli getirir; yoksa NotFound. Domain
/// <c>Product.Update</c> ile değişmezler korunur; SaveChanges ile atomik kaydedilir.
/// </summary>
internal sealed class UpdateProductHandler : ICommandHandler<UpdateProductCommand, Guid>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound);
        }

        var updated = product.Update(request.Name, request.Category, request.DefaultUnit);
        if (updated.IsFailure)
        {
            return Result.Failure<Guid>(updated.Error);
        }

        _products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}

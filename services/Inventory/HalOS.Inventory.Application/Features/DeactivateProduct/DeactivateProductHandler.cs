using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.DeactivateProduct;

/// <summary>
/// Ürünü pasifleştiren handler (soft-delete; docs/03 M2). Tenant filtreli getirir; yoksa NotFound.
/// Kayıt SİLİNMEZ — IsActive=false; geçmiş satış/mal-geliş referansları korunur.
/// </summary>
internal sealed class DeactivateProductHandler : ICommandHandler<DeactivateProductCommand, Guid>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProductHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure<Guid>(ProductErrors.NotFound);
        }

        product.Deactivate();
        _products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}

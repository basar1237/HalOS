using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Inventory.Application.Abstractions;
using HalOS.Inventory.Domain.Aggregates;

namespace HalOS.Inventory.Application.Features.CreateProduct;

/// <summary>
/// Ürün oluşturan handler (docs/03 M2). Tenant <see cref="ITenantContext"/>'ten çözülür (BK-8);
/// domain <c>Product.Create</c> ile aggregate kurulur ve SaveChanges ile atomik kaydedilir.
/// CreateWarehouseHandler deseniyle birebir.
/// </summary>
internal sealed class CreateProductHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _products;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(
        IProductRepository products,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _products = products;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var created = Product.Create(
            _tenantContext.TenantId,
            request.Name,
            request.Category,
            request.DefaultUnit);

        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        _products.Add(created.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return created.Value.Id;
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Domain.Aggregates;

namespace HalOS.Sales.Application.Features.CreateSale;

internal sealed class CreateSaleHandler : ICommandHandler<CreateSaleCommand, Guid>
{
    private readonly ISaleTransactionRepository _sales;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSaleHandler(
        ISaleTransactionRepository sales,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        IUnitOfWork unitOfWork)
    {
        _sales = sales;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        // Offline idempotency (docs/04 §5): aynı operationId ile satış zaten varsa onu döndür.
        if (request.OperationId != Guid.Empty)
        {
            var existing = await _sales.GetByOperationIdAsync(request.OperationId, cancellationToken);
            if (existing is not null)
            {
                return existing.Id;
            }
        }

        var result = SaleTransaction.Create(
            _tenantContext.TenantId,
            request.BuyerPartyId,
            request.ProducerPartyId,
            request.ConsignmentId,
            request.SoldAt,
            request.IsWithinMarket,
            request.OperationId,
            _currentUser.UserId);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _sales.Add(result.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}

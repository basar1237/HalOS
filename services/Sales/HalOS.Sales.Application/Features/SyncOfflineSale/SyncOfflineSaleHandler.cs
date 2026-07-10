using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Domain.Aggregates;

namespace HalOS.Sales.Application.Features.SyncOfflineSale;

/// <summary>
/// Offline satış senkronizasyonu (docs/04 §5). create → satırlar → complete adımlarını TEK
/// aggregate üzerinde uygular ve TEK <see cref="IUnitOfWork.SaveChangesAsync"/> ile kaydeder:
/// SaleCompleted event'i outbox'a atomik yazılır (docs/04 §10), böylece kısmi durum kalmaz.
/// Idempotency operationId ile sağlanır (tekrar oynatma çift kayıt üretmez).
/// </summary>
internal sealed class SyncOfflineSaleHandler : ICommandHandler<SyncOfflineSaleCommand, Guid>
{
    private readonly ISaleTransactionRepository _sales;
    private readonly IRateProvider _rateProvider;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public SyncOfflineSaleHandler(
        ISaleTransactionRepository sales,
        IRateProvider rateProvider,
        ITenantContext tenantContext,
        ICurrentUserContext currentUser,
        IUnitOfWork unitOfWork)
    {
        _sales = sales;
        _rateProvider = rateProvider;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(SyncOfflineSaleCommand request, CancellationToken cancellationToken)
    {
        // Idempotency (docs/04 §5): aynı operationId ile satış zaten oynatılmışsa onu döndür.
        if (request.OperationId != Guid.Empty)
        {
            var existing = await _sales.GetByOperationIdAsync(request.OperationId, cancellationToken);
            if (existing is not null)
            {
                return existing.Id;
            }
        }

        var createResult = SaleTransaction.Create(
            _tenantContext.TenantId,
            request.BuyerPartyId,
            request.ProducerPartyId,
            request.ConsignmentId,
            request.SoldAt,
            request.IsWithinMarket,
            request.OperationId,
            _currentUser.UserId,
            request.Term);

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var sale = createResult.Value;

        foreach (var line in request.Lines)
        {
            var lineResult = sale.AddLine(line.ProductId, line.Quantity, line.Unit, line.UnitPrice);
            if (lineResult.IsFailure)
            {
                return Result.Failure<Guid>(lineResult.Error);
            }
        }

        var rateResult = await _rateProvider.ResolveAsync(
            _tenantContext.TenantId,
            request.ProducerPartyId,
            request.SoldAt,
            request.IsWithinMarket,
            cancellationToken);

        if (rateResult.IsFailure)
        {
            return Result.Failure<Guid>(rateResult.Error);
        }

        var completeResult = sale.Complete(rateResult.Value);
        if (completeResult.IsFailure)
        {
            return Result.Failure<Guid>(completeResult.Error);
        }

        _sales.Add(sale);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return sale.Id;
    }
}

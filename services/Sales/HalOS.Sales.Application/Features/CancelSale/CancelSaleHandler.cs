using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Domain.Aggregates;

namespace HalOS.Sales.Application.Features.CancelSale;

/// <summary>
/// Satış iptalini yürüten handler (docs/03 §4 BK-9). Tamamlanmış satış SİLİNMEZ; domain
/// <c>Cancel</c> durumu/flag'i günceller ve SaleCancelled event'i eklenir (outbox — docs/04 §10).
/// </summary>
internal sealed class CancelSaleHandler : ICommandHandler<CancelSaleCommand>
{
    private readonly ISaleTransactionRepository _sales;
    private readonly IUnitOfWork _unitOfWork;

    public CancelSaleHandler(ISaleTransactionRepository sales, IUnitOfWork unitOfWork)
    {
        _sales = sales;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _sales.GetByIdAsync(request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result.Failure(SaleErrors.NotFound);
        }

        var result = sale.Cancel(request.Reason);
        if (result.IsFailure)
        {
            return result;
        }

        _sales.Update(sale);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

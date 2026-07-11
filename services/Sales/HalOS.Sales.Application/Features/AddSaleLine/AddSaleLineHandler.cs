using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Domain.Aggregates;

namespace HalOS.Sales.Application.Features.AddSaleLine;

internal sealed class AddSaleLineHandler : ICommandHandler<AddSaleLineCommand>
{
    private readonly ISaleTransactionRepository _sales;
    private readonly IUnitOfWork _unitOfWork;

    public AddSaleLineHandler(ISaleTransactionRepository sales, IUnitOfWork unitOfWork)
    {
        _sales = sales;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddSaleLineCommand request, CancellationToken cancellationToken)
    {
        var sale = await _sales.GetByIdAsync(request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result.Failure(SaleErrors.NotFound);
        }

        var result = sale.AddLine(request.ProductId, request.Quantity, request.Unit, request.UnitPrice);
        if (result.IsFailure)
        {
            return result;
        }

        // sale İZLENEN (tracked) yüklenir; kökteki GrossAmount değişikliğini change tracking algılar.
        // Yeni SaleLine client-generated Guid ID taşıdığından EF onu yanlışlıkla "Modified" sayar
        // (UPDATE → 0 satır → DbUpdateConcurrencyException); bu yüzden yeni satırı açıkça "Added"
        // olarak bildiririz. _sales.Update(sale) ÇAĞRILMAZ (tüm grafiği Modified işaretlerdi).
        _sales.RegisterNew(sale.Lines.Last());
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

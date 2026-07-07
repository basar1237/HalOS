using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Contracts;
using HalOS.Sales.Domain.Aggregates;

namespace HalOS.Sales.Application.Features.GetSale;

internal sealed class GetSaleHandler : IQueryHandler<GetSaleQuery, SaleDto>
{
    private readonly ISaleTransactionRepository _sales;

    public GetSaleHandler(ISaleTransactionRepository sales)
    {
        _sales = sales;
    }

    public async Task<Result<SaleDto>> Handle(GetSaleQuery request, CancellationToken cancellationToken)
    {
        var sale = await _sales.GetByIdAsync(request.SaleId, cancellationToken);
        if (sale is null)
        {
            return Result.Failure<SaleDto>(SaleErrors.NotFound);
        }

        return SaleDto.FromDomain(sale);
    }
}

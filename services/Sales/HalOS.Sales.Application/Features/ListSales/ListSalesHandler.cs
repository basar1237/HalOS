using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Sales.Application.Abstractions;
using HalOS.Sales.Application.Contracts;

namespace HalOS.Sales.Application.Features.ListSales;

internal sealed class ListSalesHandler : IQueryHandler<ListSalesQuery, PagedResult<SaleDto>>
{
    private readonly ISaleTransactionRepository _sales;

    public ListSalesHandler(ISaleTransactionRepository sales)
    {
        _sales = sales;
    }

    public async Task<Result<PagedResult<SaleDto>>> Handle(ListSalesQuery request, CancellationToken cancellationToken)
    {
        var page = await _sales.ListAsync(
            request.Page, request.PageSize, request.From, request.To, cancellationToken);

        var dto = new PagedResult<SaleDto>(
            page.Items.Select(SaleDto.FromDomain).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);

        return dto;
    }
}

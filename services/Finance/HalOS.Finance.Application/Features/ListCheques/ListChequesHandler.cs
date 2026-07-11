using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Contracts;

namespace HalOS.Finance.Application.Features.ListCheques;

internal sealed class ListChequesHandler : IQueryHandler<ListChequesQuery, PagedResult<ChequeDto>>
{
    private readonly IChequeRepository _cheques;

    public ListChequesHandler(IChequeRepository cheques)
    {
        _cheques = cheques;
    }

    public async Task<Result<PagedResult<ChequeDto>>> Handle(ListChequesQuery request, CancellationToken cancellationToken)
    {
        var page = await _cheques.ListAsync(request.Page, request.PageSize, cancellationToken);
        return new PagedResult<ChequeDto>(
            page.Items.Select(ChequeDto.FromDomain).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Finance.Application.Abstractions;
using HalOS.Finance.Application.Contracts;

namespace HalOS.Finance.Application.Features.ListCurrentAccounts;

/// <summary>Sayfalanmış cari hesap listesi query handler (docs/03 M6). Tenant filtreli (BK-8).</summary>
internal sealed class ListCurrentAccountsHandler
    : IQueryHandler<ListCurrentAccountsQuery, PagedResult<CurrentAccountDto>>
{
    private readonly ICurrentAccountRepository _accounts;

    public ListCurrentAccountsHandler(ICurrentAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<Result<PagedResult<CurrentAccountDto>>> Handle(
        ListCurrentAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _accounts.ListAsync(request.Page, request.PageSize, cancellationToken);

        var dto = new PagedResult<CurrentAccountDto>(
            page.Items.Select(CurrentAccountDto.FromDomain).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);

        return dto;
    }
}

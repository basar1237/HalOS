using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.ListProductPassports;

/// <summary>Sayfalanmış künye listesini döndüren query handler (docs/03 §5). Tenant filtreli (BK-8).</summary>
internal sealed class ListProductPassportsHandler
    : IQueryHandler<ListProductPassportsQuery, PagedResult<ProductPassportDto>>
{
    private readonly IProductPassportRepository _passports;

    public ListProductPassportsHandler(IProductPassportRepository passports)
    {
        _passports = passports;
    }

    public async Task<Result<PagedResult<ProductPassportDto>>> Handle(
        ListProductPassportsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _passports.ListAsync(request.Page, request.PageSize, cancellationToken);

        var items = page.Items.Select(ProductPassportDto.FromDomain).ToList();

        return new PagedResult<ProductPassportDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.ListProducerReceipts;

/// <summary>Sayfalanmış e-MM listesini döndüren query handler (docs/03 M7). Tenant filtreli (BK-8).</summary>
internal sealed class ListProducerReceiptsHandler
    : IQueryHandler<ListProducerReceiptsQuery, PagedResult<ProducerReceiptDto>>
{
    private readonly IProducerReceiptRepository _receipts;

    public ListProducerReceiptsHandler(IProducerReceiptRepository receipts)
    {
        _receipts = receipts;
    }

    public async Task<Result<PagedResult<ProducerReceiptDto>>> Handle(
        ListProducerReceiptsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _receipts.ListAsync(request.Page, request.PageSize, cancellationToken);

        var items = page.Items.Select(ProducerReceiptDto.FromDomain).ToList();

        return new PagedResult<ProducerReceiptDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

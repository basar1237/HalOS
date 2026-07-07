using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.ListInvoices;

/// <summary>Sayfalanmış e-Fatura listesini döndüren query handler (docs/03 M8). Tenant filtreli (BK-8).</summary>
internal sealed class ListInvoicesHandler
    : IQueryHandler<ListInvoicesQuery, PagedResult<InvoiceDto>>
{
    private readonly IInvoiceRepository _invoices;

    public ListInvoicesHandler(IInvoiceRepository invoices)
    {
        _invoices = invoices;
    }

    public async Task<Result<PagedResult<InvoiceDto>>> Handle(
        ListInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _invoices.ListAsync(request.Page, request.PageSize, cancellationToken);

        var items = page.Items.Select(InvoiceDto.FromDomain).ToList();

        return new PagedResult<InvoiceDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

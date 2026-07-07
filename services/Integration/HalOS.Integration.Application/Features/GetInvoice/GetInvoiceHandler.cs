using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;

namespace HalOS.Integration.Application.Features.GetInvoice;

/// <summary>e-Fatura'yı kimliğiyle getiren query handler (docs/03 M8). Tenant filtreli (BK-8).</summary>
internal sealed class GetInvoiceHandler : IQueryHandler<GetInvoiceQuery, InvoiceDto>
{
    private readonly IInvoiceRepository _invoices;

    public GetInvoiceHandler(IInvoiceRepository invoices)
    {
        _invoices = invoices;
    }

    public async Task<Result<InvoiceDto>> Handle(GetInvoiceQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<InvoiceDto>(Invoice.InvoiceErrors.NotFound);
        }

        return InvoiceDto.FromDomain(invoice);
    }
}

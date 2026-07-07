using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Contracts;
using HalOS.Integration.Domain.Aggregates;

namespace HalOS.Integration.Application.Features.GetProducerReceipt;

/// <summary>e-MM'i kimliğiyle getiren query handler (docs/03 M7). Tenant filtreli (BK-8).</summary>
internal sealed class GetProducerReceiptHandler : IQueryHandler<GetProducerReceiptQuery, ProducerReceiptDto>
{
    private readonly IProducerReceiptRepository _receipts;

    public GetProducerReceiptHandler(IProducerReceiptRepository receipts)
    {
        _receipts = receipts;
    }

    public async Task<Result<ProducerReceiptDto>> Handle(GetProducerReceiptQuery request, CancellationToken cancellationToken)
    {
        var receipt = await _receipts.GetByIdAsync(request.ReceiptId, cancellationToken);
        if (receipt is null)
        {
            return Result.Failure<ProducerReceiptDto>(ProducerReceipt.ProducerReceiptErrors.NotFound);
        }

        return ProducerReceiptDto.FromDomain(receipt);
    }
}

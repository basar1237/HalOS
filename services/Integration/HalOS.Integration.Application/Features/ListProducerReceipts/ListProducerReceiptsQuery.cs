using HalOS.BuildingBlocks.Application;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.ListProducerReceipts;

/// <summary>
/// Tenant filtreli, sayfalanmış e-MM listesi (docs/03 §5 e-Belge Merkezi; docs/03 M7).
/// </summary>
public sealed record ListProducerReceiptsQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ProducerReceiptDto>>;

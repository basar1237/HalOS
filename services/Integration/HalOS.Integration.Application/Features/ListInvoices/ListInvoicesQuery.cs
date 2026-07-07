using HalOS.BuildingBlocks.Application;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.ListInvoices;

/// <summary>
/// Tenant filtreli, sayfalanmış e-Fatura (HAL) listesi (docs/03 §5 e-Belge Merkezi; docs/03 M8).
/// </summary>
public sealed record ListInvoicesQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<InvoiceDto>>;

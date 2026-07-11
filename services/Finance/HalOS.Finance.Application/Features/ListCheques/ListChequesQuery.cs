using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Application.Contracts;

namespace HalOS.Finance.Application.Features.ListCheques;

/// <summary>Tenant filtreli, sayfalanmış çek/senet listesi (vadeye göre).</summary>
public sealed record ListChequesQuery(int Page = 1, int PageSize = 50) : IQuery<PagedResult<ChequeDto>>;

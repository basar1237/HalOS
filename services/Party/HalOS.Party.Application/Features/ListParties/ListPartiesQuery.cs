using HalOS.BuildingBlocks.Application;
using HalOS.Party.Application.Contracts;

namespace HalOS.Party.Application.Features.ListParties;

/// <summary>Basit sayfalanmış taraf listesi (docs/03 M1). Tenant filtreli (BK-8).</summary>
public sealed record ListPartiesQuery(int Page = 1, int PageSize = 20, bool OnlyActive = true)
    : IQuery<PagedResult<PartyDto>>;

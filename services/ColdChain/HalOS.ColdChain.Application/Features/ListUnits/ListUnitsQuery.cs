using HalOS.BuildingBlocks.Application;
using HalOS.ColdChain.Application.Contracts;

namespace HalOS.ColdChain.Application.Features.ListUnits;

/// <summary>Sayfalanmış soğuk hava deposu listesi (ada göre sıralı; docs/04 §6).</summary>
public sealed record ListUnitsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<ColdStorageUnitDto>>;

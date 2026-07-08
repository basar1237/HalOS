using HalOS.BuildingBlocks.Application;
using HalOS.Integration.Application.Contracts;

namespace HalOS.Integration.Application.Features.ListProductPassports;

/// <summary>Tenant filtreli, sayfalanmış künye listesi (docs/03 §5 e-Belge Merkezi).</summary>
public sealed record ListProductPassportsQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ProductPassportDto>>;

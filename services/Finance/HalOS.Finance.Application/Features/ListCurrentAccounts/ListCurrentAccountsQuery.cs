using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Application.Contracts;

namespace HalOS.Finance.Application.Features.ListCurrentAccounts;

/// <summary>
/// Tenant filtreli, sayfalanmış cari hesap listesi (docs/03 §5 "Cari Kartları"; docs/03 M6).
/// Her satır bakiye özetini taşır.
/// </summary>
public sealed record ListCurrentAccountsQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<CurrentAccountDto>>;

using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Application.Contracts;

namespace HalOS.Finance.Application.Features.GetCurrentAccount;

/// <summary>
/// Bir tarafın (Party) cari hesabını bakiyesiyle getirir (docs/03 M6; docs/05 §3.7 cari 1:1 party).
/// </summary>
public sealed record GetCurrentAccountQuery(Guid PartyId) : IQuery<CurrentAccountDto>;

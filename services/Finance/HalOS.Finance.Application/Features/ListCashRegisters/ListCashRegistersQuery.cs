using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Application.Contracts;

namespace HalOS.Finance.Application.Features.ListCashRegisters;

/// <summary>Tenant filtreli kasa listesi (bakiye özetiyle).</summary>
public sealed record ListCashRegistersQuery : IQuery<IReadOnlyList<CashRegisterDto>>;

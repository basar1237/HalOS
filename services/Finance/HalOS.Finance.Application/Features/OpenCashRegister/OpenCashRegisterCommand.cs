using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.OpenCashRegister;

/// <summary>Yeni kasa açar (ticari/rehin) — docs/11 §3.6.</summary>
public sealed record OpenCashRegisterCommand(string Name, CashRegisterKind Kind) : ICommand<Guid>;

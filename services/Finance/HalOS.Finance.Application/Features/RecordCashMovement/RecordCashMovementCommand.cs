using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.RecordCashMovement;

/// <summary>Kasaya tahsil/tediye hareketi işler (docs/11 §3.6).</summary>
public sealed record RecordCashMovementCommand(
    Guid RegisterId,
    CashDirection Direction,
    decimal Amount,
    string? Description,
    DateTime OccurredAt) : ICommand<Guid>;

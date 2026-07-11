using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Api.Contracts;

public sealed record OpenCashRegisterRequest(string Name, CashRegisterKind Kind);

public sealed record RecordCashMovementRequest(
    CashDirection Direction,
    decimal Amount,
    string? Description,
    DateTime? OccurredAt);

public sealed record CashTransferRequest(
    Guid FromRegisterId,
    Guid ToRegisterId,
    decimal Amount,
    string? Description,
    DateTime? OccurredAt);

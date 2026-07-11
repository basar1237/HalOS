using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Api.Contracts;

public sealed record RegisterChequeRequest(
    ChequeKind Kind,
    ChequeDirection Direction,
    Guid? PartyId,
    string? BankName,
    string? SerialNo,
    decimal Amount,
    DateTime IssueDate,
    DateTime DueDate,
    string? Note);

public sealed record ChangeChequeStatusRequest(ChequeStatus NewStatus);

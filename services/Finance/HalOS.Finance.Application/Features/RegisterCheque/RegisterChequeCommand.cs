using HalOS.BuildingBlocks.Application;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Features.RegisterCheque;

/// <summary>Portföye çek/senet kaydeder (docs/11 §3.5).</summary>
public sealed record RegisterChequeCommand(
    ChequeKind Kind,
    ChequeDirection Direction,
    Guid? PartyId,
    string? BankName,
    string? SerialNo,
    decimal Amount,
    DateTime IssueDate,
    DateTime DueDate,
    string? Note) : ICommand<Guid>;

using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Contracts;

/// <summary>Çek/Senet okuma modeli (liste/detay).</summary>
public sealed record ChequeDto(
    Guid Id,
    int Kind,
    int Direction,
    Guid? PartyId,
    string BankName,
    string SerialNo,
    decimal Amount,
    DateTime IssueDate,
    DateTime DueDate,
    int Status,
    string? Note)
{
    public static ChequeDto FromDomain(Cheque c) => new(
        c.Id,
        (int)c.Kind,
        (int)c.Direction,
        c.PartyId,
        c.BankName,
        c.SerialNo,
        c.Amount,
        c.IssueDate,
        c.DueDate,
        (int)c.Status,
        c.Note);
}

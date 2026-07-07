using HalOS.Finance.Domain.Aggregates;
using HalOS.Finance.Domain.Enums;

namespace HalOS.Finance.Application.Contracts;

/// <summary>Cari hareket okuma DTO'su (docs/05 §3.7 <c>account_entry</c>).</summary>
public sealed record AccountEntryDto(
    Guid Id,
    EntryDirection Direction,
    EntryType Type,
    decimal Amount,
    decimal SignedAmount,
    Guid? RefId,
    DateTime OccurredAt,
    DateTime? DueDate)
{
    public static AccountEntryDto FromDomain(AccountEntry entry) => new(
        entry.Id,
        entry.Direction,
        entry.Type,
        entry.Amount,
        entry.SignedAmount,
        entry.RefId,
        entry.OccurredAt,
        entry.DueDate);
}

/// <summary>
/// Cari hesap okuma DTO'su (docs/05 §3.7 <c>current_account</c>). Bakiye türetilmiş değerdir
/// (docs/02 §3.4). Domain aggregate'i API'ye sızmaz.
/// </summary>
public sealed record CurrentAccountDto(
    Guid Id,
    Guid TenantId,
    Guid PartyId,
    decimal Balance,
    int EntryCount)
{
    public static CurrentAccountDto FromDomain(CurrentAccount account) => new(
        account.Id,
        account.TenantId,
        account.PartyId,
        account.Balance,
        account.Entries.Count);
}

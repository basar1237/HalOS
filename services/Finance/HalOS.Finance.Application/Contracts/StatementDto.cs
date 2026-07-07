using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Contracts;

/// <summary>
/// Cari ekstre (hesap özeti) DTO'su (docs/03 §5 "Cari Detay/Ekstre"): hesap kimliği, taraf,
/// güncel bakiye ve hareketler (oluşma zamanına göre artan). Ekstre &lt; 1 sn hedefine uygun
/// hafif projeksiyon (docs/03 §7 performans).
/// </summary>
public sealed record StatementDto(
    Guid CurrentAccountId,
    Guid PartyId,
    decimal Balance,
    IReadOnlyList<AccountEntryDto> Entries)
{
    public static StatementDto FromDomain(CurrentAccount account) => new(
        account.Id,
        account.PartyId,
        account.Balance,
        account.Entries
            .OrderBy(e => e.OccurredAt)
            .Select(AccountEntryDto.FromDomain)
            .ToList());
}

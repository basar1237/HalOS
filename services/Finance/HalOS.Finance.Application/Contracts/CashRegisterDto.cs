using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Contracts;

/// <summary>Kasa okuma modeli (liste). Bakiye Σ hareket ile türetilir.</summary>
public sealed record CashRegisterDto(
    Guid Id,
    string Name,
    int Kind,
    decimal Balance,
    int MovementCount)
{
    public static CashRegisterDto FromDomain(CashRegister r) =>
        new(r.Id, r.Name, (int)r.Kind, r.Balance, r.Movements.Count);
}

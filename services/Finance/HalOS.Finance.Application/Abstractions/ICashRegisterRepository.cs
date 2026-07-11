using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Abstractions;

/// <summary>Kasa persistence port'u. Tenant global query filter'a tabidir (BK-8).</summary>
public interface ICashRegisterRepository
{
    void Add(CashRegister register);
    Task<CashRegister?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashRegister>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// İzlenen kasaya EKLENEN yeni CashMovement'i EF'e açıkça "Added" bildirir (client-generated
    /// Guid ID'li çocuk aksi halde "Modified" sanılır → UPDATE 0 satır hatası; Sales deseniyle aynı).
    /// </summary>
    void RegisterNew(object child);
}

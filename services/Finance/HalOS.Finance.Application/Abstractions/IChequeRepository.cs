using HalOS.Finance.Application.Contracts;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Abstractions;

/// <summary>Çek/Senet persistence port'u. Tenant global query filter'a tabidir (BK-8).</summary>
public interface IChequeRepository
{
    void Add(Cheque cheque);
    void Update(Cheque cheque);
    Task<Cheque?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Cheque>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

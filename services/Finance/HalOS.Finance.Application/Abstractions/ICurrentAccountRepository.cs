using HalOS.Finance.Application.Contracts;
using HalOS.Finance.Domain.Aggregates;

namespace HalOS.Finance.Application.Abstractions;

/// <summary>
/// CurrentAccount aggregate persistence port'u. Tüm sorgular tenant global query filter'a tabidir
/// (BK-8). Hareketler (AccountEntry) aggregate ile birlikte yüklenir; bakiye türetildiğinden
/// (docs/02 §3.4) hareket koleksiyonu iş metotları için gereklidir.
/// </summary>
public interface ICurrentAccountRepository
{
    /// <summary>Cari hesabı hareketleriyle birlikte getirir (tenant filtreli).</summary>
    Task<CurrentAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir tarafın (Party) cari hesabını hareketleriyle getirir; yoksa null. Cari 1:1
    /// party (docs/05 §3.7).
    /// </summary>
    Task<CurrentAccount?> GetByPartyIdAsync(Guid partyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant filtreli, sayfalanmış cari hesap listesi. Hareketler dahil (bakiye türetimi için).
    /// </summary>
    Task<PagedResult<CurrentAccount>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(CurrentAccount account);

    void Update(CurrentAccount account);
}

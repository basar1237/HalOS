using HalOS.Party.Application.Contracts;
using PartyAggregate = HalOS.Party.Domain.Aggregates.Party;

namespace HalOS.Party.Application.Abstractions;

/// <summary>Party aggregate persistence port'u. Tüm sorgular tenant global query filter'a tabidir (BK-8).</summary>
public interface IPartyRepository
{
    Task<PartyAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Verilen TCKN bu tenant'ta zaten var mı (tekillik ön-kontrolü, docs/02 §3.1).</summary>
    Task<bool> ExistsByTcknAsync(string tckn, CancellationToken cancellationToken = default);

    /// <summary>Verilen VKN bu tenant'ta zaten var mı (tekillik ön-kontrolü, docs/02 §3.1).</summary>
    Task<bool> ExistsByVknAsync(string vkn, CancellationToken cancellationToken = default);

    /// <summary>Basit sayfalanmış liste (tenant filtreli). Toplam kayıt sayısıyla birlikte döner.</summary>
    Task<PagedResult<PartyAggregate>> ListAsync(
        int page,
        int pageSize,
        bool onlyActive,
        CancellationToken cancellationToken = default);

    void Add(PartyAggregate party);

    void Update(PartyAggregate party);
}

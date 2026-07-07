using HalOS.BuildingBlocks.Application;

namespace HalOS.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// İki tenant kaynağını birleştiren <see cref="ITenantContext"/>: birincil (HTTP/JWT tabanlı,
/// istek scope'unda) ve <see cref="AmbientTenantContext"/> (broker mesajından
/// <see cref="TenantConsumeFilter{T}"/> ile doldurulan consumer scope'u). Bir HTTP isteği
/// işleniyorsa birincil tenant çözülür; consumer scope'unda HTTP bağlamı olmadığından birincil
/// <c>HasTenant=false</c> olur ve ambient tenant devreye girer (docs/07 §6 / BK-8).
///
/// Mesajlaşma tüketen servisler (Sales oran-senkronu, Finance) API kompozisyon kökünde
/// <c>ITenantContext</c>'i bu tipe bağlar; birincil kaynak servise özgü HTTP tenant context'idir.
/// </summary>
public sealed class CompositeTenantContext : ITenantContext
{
    private readonly ITenantContext _primary;
    private readonly AmbientTenantContext _ambient;

    /// <param name="primary">Birincil (HTTP/JWT) tenant kaynağı — istek scope'unda çözülür.</param>
    /// <param name="ambient">Broker consumer scope'unda mesajdan doldurulan tenant kaynağı.</param>
    public CompositeTenantContext(ITenantContext primary, AmbientTenantContext ambient)
    {
        _primary = primary;
        _ambient = ambient;
    }

    /// <inheritdoc />
    public Guid TenantId => _primary.HasTenant ? _primary.TenantId : _ambient.TenantId;

    /// <inheritdoc />
    public bool HasTenant => _primary.HasTenant || _ambient.HasTenant;
}

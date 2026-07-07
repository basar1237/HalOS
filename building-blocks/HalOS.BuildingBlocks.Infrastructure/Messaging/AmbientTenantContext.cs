using HalOS.BuildingBlocks.Application;

namespace HalOS.BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Set edilebilir (mutable), scope-başına bir <see cref="ITenantContext"/> uygulaması. HTTP
/// bağlamının OLMADIĞI durumlarda — özellikle bir MassTransit consumer'ı mesajı işlerken —
/// tenant'ı taşımak için kullanılır. <see cref="TenantConsumeFilter"/> gelen mesaj
/// <see cref="Contracts.ITenantScopedEvent"/> ise <see cref="SetTenant"/> ile bu bağlamı doldurur;
/// böylece consumer'ın açtığı <c>TenantDbContextBase</c> global query filter'ı doğru tenant'ta
/// çalışır ve <c>SaveChanges</c> öncesi izolasyon korunur (docs/07 §6 / BK-8).
///
/// API kompozisyonunda birincil <see cref="ITenantContext"/> HTTP tabanlı olabilir; bu tür
/// consumer scope'unda ambient bağlam devreye girer (bkz. DI yardımcısı notu).
/// </summary>
public sealed class AmbientTenantContext : ITenantContext
{
    private Guid _tenantId;
    private bool _hasTenant;

    /// <inheritdoc />
    public Guid TenantId => _tenantId;

    /// <inheritdoc />
    public bool HasTenant => _hasTenant;

    /// <summary>
    /// Geçerli scope için tenant'ı ayarlar. Consume filter mesajdan çözülen tenant ile çağırır.
    /// </summary>
    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
        _hasTenant = tenantId != Guid.Empty;
    }
}

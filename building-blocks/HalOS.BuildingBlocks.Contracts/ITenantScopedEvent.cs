namespace HalOS.BuildingBlocks.Contracts;

/// <summary>
/// Servisler-arası (cross-service) yayınlanan ve belirli bir kiracıya (tenant) ait olan
/// event'ler için işaret arayüzü. Consumer tarafı, gelen mesajı işlerken doğru tenant
/// bağlamını (ambient tenant) bu <see cref="TenantId"/>'den kurar — böylece
/// <c>SaveChanges</c> öncesi çok-kiracılı izolasyon korunur (docs/07 §6 / BK-8).
/// Broker üzerinden geçen mesajda HTTP/JWT bağlamı olmadığından tenant'ı event'in kendisi
/// taşımak zorundadır (docs/04 §10, ADR-008).
/// </summary>
public interface ITenantScopedEvent
{
    /// <summary>Event'in ait olduğu kiracı (tenant) kimliği.</summary>
    Guid TenantId { get; }
}

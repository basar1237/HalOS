namespace HalOS.BuildingBlocks.Application;

/// <summary>
/// Uygulama katmanının denetim kaydı taşıyıcısı (kim/ne/ne zaman — docs/05 §3.11). Pipeline
/// behavior bunu kurar; <see cref="IAuditLogSink"/> kalıcılaştırır. EF entity'si (audit_log)
/// Infrastructure'dadır; Application → Infrastructure bağımlılığı olmaması için behavior o
/// entity'ye değil bu düz taşıyıcıya yazar (sink taşıyıcıyı entity'ye eşler). Alanlar,
/// docs/05 §3.11 audit_log kolonlarıyla birebir örtüşür.
/// </summary>
/// <param name="TenantId">Kaydın tenant'ı (docs/07 §6); yoksa null.</param>
/// <param name="UserId">Komutu yürüten kullanıcı; yoksa null.</param>
/// <param name="Action">Yürütülen eylem — komut CLR tip adı.</param>
/// <param name="EntityType">İlgili entity tipi adı (varsa).</param>
/// <param name="EntityId">İlgili entity kimliği (varsa).</param>
/// <param name="BeforeJson">Değişiklik öncesi durum (JSON; varsa).</param>
/// <param name="AfterJson">Değişiklik sonrası durum (JSON; varsa).</param>
/// <param name="CreatedOnUtc">Kaydın oluşturulduğu an (UTC).</param>
public sealed record AuditEntry(
    Guid? TenantId,
    Guid? UserId,
    string Action,
    string? EntityType,
    string? EntityId,
    string? BeforeJson,
    string? AfterJson,
    DateTime CreatedOnUtc);

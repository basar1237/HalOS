namespace HalOS.BuildingBlocks.Application;

/// <summary>
/// Denetim kayıtlarını kalıcılaştırır (append-only — docs/04 §201, docs/05 §3.11). Behavior
/// kaydı <see cref="Add"/> ile evreler, ardından <see cref="SaveAsync"/> ile yazar. Somut
/// uygulaması (Infrastructure) taşıyıcıyı <c>audit_log</c> EF entity'sine eşler; bu arayüz
/// Application katmanında olduğundan Infrastructure'a bağımlılık doğmaz (IOutboxWriter deseniyle
/// paralel; audit için ayrı, best-effort ikinci save — bkz. <see cref="AuditLoggingBehavior{TRequest,TResponse}"/>).
/// </summary>
public interface IAuditLogSink
{
    /// <summary>Bir denetim kaydını yazılmak üzere evreler.</summary>
    void Add(AuditEntry entry);

    /// <summary>Evrelenmiş denetim kayıtlarını kalıcılaştırır.</summary>
    Task SaveAsync(CancellationToken ct);
}

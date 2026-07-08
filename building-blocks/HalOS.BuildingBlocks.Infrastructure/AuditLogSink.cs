using HalOS.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;

namespace HalOS.BuildingBlocks.Infrastructure;

/// <summary>
/// <see cref="IAuditLogSink"/>'in EF Core uygulaması: Application katmanının <see cref="AuditEntry"/>
/// taşıyıcısını <c>audit_log</c> EF entity'sine (<see cref="AuditLog"/>) eşler ve servisin kendi
/// <typeparamref name="TContext"/>'i üzerinden kalıcılaştırır. Append-only (docs/04 §201): yalnız
/// ekleme. Komutun kendi save'inden ayrı, ikinci bir <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
/// çağrısıdır (best-effort denetim, docs/05 §3.11). Her servis bunu <c>AuditLogSink&lt;XDbContext&gt;</c>
/// olarak scoped kaydeder — outbox/tenant deseniyle paralel, tek merkezi implementasyon.
///
/// <para><b>Paylaşılan-context değişmezi (invariant):</b> Bu sink, komutun handler'ıyla AYNI
/// scoped <typeparamref name="TContext"/> örneğini kullanır. <see cref="AuditLoggingBehavior{TRequest,TResponse}"/>
/// denetimi handler NORMAL döndükten SONRA çağırdığından, handler kendi Unit of Work'ünü (state +
/// outbox) ZATEN kaydetmiş olmalıdır; audit save'i çalıştığında context'te BEKLEYEN başka izlenen
/// değişiklik kalmamalıdır. Aksi halde bu ikinci <c>SaveChangesAsync</c> o değişiklikleri (örn.
/// henüz kaydedilmemiş outbox satırlarını) de yazar — bilinçsiz kaçak yazım tuzağı. Konvansiyon:
/// komut handler'ları döndürmeden önce daima save eder (mevcut IUnitOfWork deseni). Faz 2'de audit
/// ile state'in tek transaction'da atomik yazımı bu değişmezi gereksiz kılacaktır.</para>
/// </summary>
/// <typeparam name="TContext">Servisin tenant'lı DbContext'i (audit_log tablosunu içerir).</typeparam>
public sealed class AuditLogSink<TContext> : IAuditLogSink
    where TContext : TenantDbContextBase
{
    private readonly TContext _context;

    public AuditLogSink(TContext context)
    {
        _context = context;
    }

    public void Add(AuditEntry entry)
    {
        _context.Set<AuditLog>().Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = entry.TenantId,
            UserId = entry.UserId,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            BeforeJson = entry.BeforeJson,
            AfterJson = entry.AfterJson,
            CreatedOnUtc = entry.CreatedOnUtc
        });
    }

    public Task SaveAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}

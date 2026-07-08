using MediatR;
using Microsoft.Extensions.Logging;

namespace HalOS.BuildingBlocks.Application;

/// <summary>
/// MediatR pipeline behavior'ı: BAŞARIYLA YÜRÜTÜLEN her KOMUT (<see cref="ICommand"/> /
/// <see cref="ICommand{T}"/>) için kalıcı bir denetim kaydı yazar (kim/ne/ne zaman — docs/05 §3.11,
/// docs/03 §6, docs/04 §201). Yalnız komutlar denetlenir; QUERY'ler (<see cref="IQuery{T}"/>)
/// denetlenmez.
///
/// <para><b>Faz 1 kapsamı (önemli):</b> Denetim kaydı <c>next()</c> NORMAL döndükten SONRA yazılır.
/// Dolayısıyla yalnızca handler'a ulaşıp normal dönen komutlar denetlenir. Bu behavior
/// <see cref="ValidationBehavior{TRequest,TResponse}"/>'dan SONRA (daha İÇTE) kayıtlı olduğundan iki
/// başarısızlık modu Faz 1'de denetlenMEZ:
/// (1) FluentValidation başarısız olursa ValidationBehavior <c>next()</c>'i ÇAĞIRMADAN başarısız
///     sonuç döndürür → bu behavior hiç çalışmaz, reddedilen deneme kaydedilmez.
/// (2) Handler bir istisna FIRLATIRSA <c>next()</c> istisna atar → denetim satırı yazılmaz.
/// (İstisna atmadan başarısız <see cref="Result"/> DÖNDÜREN bir handler ise denetlenir; çünkü
/// <c>next()</c> normal döner.) Reddedilen/hatalı denemelerin de (outcome/status alanıyla)
/// denetlenmesi — ve denetimin state ile AYNI transaction'da atomik yazılması — bilinçli olarak
/// <b>Faz 2'ye</b> bırakılmıştır (kapsam kararı, plan sahibi onaylı).</para>
///
/// <para><b>Best-effort yazım:</b> Denetim, komutun kendi save'inden AYRI ikinci bir save ile
/// (state değişikliğiyle aynı transaction'da DEĞİL) yazılır. Bu ikinci save başarısız olursa
/// istisna YUTULUR ve loglanır — böylece ZATEN COMMIT'lenmiş komut sonucu bir denetim-yazım hatası
/// yüzünden maskelenmez/geri alınmaz (docs/07 §10 hata yönetimi + best-effort denetim, docs/05 §3.11).
/// Tam transaction'lı denetim Faz 2'dedir. ValidationBehavior deseniyle birebir (sealed, açık
/// pipeline behavior).</para>
/// </summary>
public sealed class AuditLoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAuditLogSink _sink;
    private readonly IAuditActor _actor;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuditLoggingBehavior<TRequest, TResponse>> _logger;

    public AuditLoggingBehavior(
        IAuditLogSink sink,
        IAuditActor actor,
        ITenantContext tenantContext,
        ILogger<AuditLoggingBehavior<TRequest, TResponse>> logger)
    {
        _sink = sink;
        _actor = actor;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Yalnız komutları denetle; query'ler (okuma) denetlenmez (docs/07 §5 CQRS).
        if (!IsCommand())
        {
            return await next();
        }

        // Önce komutu çalıştır; audit komut SONRASI ayrı save ile yazılır (best-effort, Faz 1).
        // next() istisna atarsa buradan itibaren ilerlenmez → hatalı deneme Faz 1'de denetlenmez
        // (bilinçli kapsam kararı; bkz. sınıf XML doc'u ve Faz 2).
        var response = await next();

        var entry = new AuditEntry(
            TenantId: _tenantContext.HasTenant ? _tenantContext.TenantId : null,
            UserId: _actor.HasUser ? _actor.UserId : null,
            Action: typeof(TRequest).Name,
            EntityType: null,
            EntityId: null,
            BeforeJson: null,
            AfterJson: null,
            CreatedOnUtc: DateTime.UtcNow);

        // Best-effort: denetim yazımı komut save'inden AYRI ikinci bir save'dir. Bu save patlarsa
        // istisnayı YUTUP logla; aksi halde ZATEN COMMIT'lenmiş komut sonucu maskelenir (docs/07 §10).
        try
        {
            _sink.Add(entry);
            await _sink.SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Denetim kaydı yazılamadı (komut zaten uygulandı); Action={AuditAction}, TenantId={AuditTenantId}.",
                entry.Action,
                entry.TenantId);
        }

        return response;
    }

    /// <summary>
    /// <typeparamref name="TRequest"/> bir komut mu? (<see cref="ICommand"/> veya generic
    /// <see cref="ICommand{T}"/> arayüzlerinden birini uyguluyorsa). Query'ler
    /// (<see cref="IQuery{T}"/>) bu kontrolü geçemez ve denetlenmez.
    /// </summary>
    private static bool IsCommand()
    {
        foreach (var iface in typeof(TRequest).GetInterfaces())
        {
            if (iface == typeof(ICommand))
            {
                return true;
            }

            if (iface.IsGenericType &&
                iface.GetGenericTypeDefinition() == typeof(ICommand<>))
            {
                return true;
            }
        }

        return false;
    }
}

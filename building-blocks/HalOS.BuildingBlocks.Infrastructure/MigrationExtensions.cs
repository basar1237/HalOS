using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HalOS.BuildingBlocks.Infrastructure;

/// <summary>
/// Açılışta bekleyen EF Core migration'larını uygulayan yardımcı (docs/04 §9 tek-komut dev).
/// Her servis <c>var app = builder.Build();</c> sonrası <c>app.Services.ApplyMigrations&lt;TContext&gt;()</c>
/// çağırır; böylece Docker ile ayağa kalkan boş veritabanı şeması otomatik oluşur (postgres-init
/// yalnız DB'leri açar, tabloları değil). Migrate() idempotenttir — uygulanmış migration tekrar
/// çalışmaz. Postgres henüz hazır değilse kısa bir yeniden-deneme ile beklenir (compose sırası).
/// </summary>
public static class MigrationExtensions
{
    public static void ApplyMigrations<TContext>(this IServiceProvider services, int maxAttempts = 10)
        where TContext : DbContext
    {
        using var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<TContext>();
        var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("HalOS.Migrations");

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                db.Database.Migrate();
                logger?.LogInformation("{Context}: migration'lar uygulandı.", typeof(TContext).Name);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                // Postgres/ağ henüz hazır olmayabilir (compose başlatma sırası) — kısa bekleyip yeniden dene.
                logger?.LogWarning(
                    "{Context}: migration denemesi {Attempt}/{Max} başarısız: {Message}. Yeniden denenecek.",
                    typeof(TContext).Name, attempt, maxAttempts, ex.Message);
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }
    }
}

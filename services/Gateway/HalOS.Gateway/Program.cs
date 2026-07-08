using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog: yapısal loglama → konsol + (yapılandırılmışsa) Seq (docs/04 §8).
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// --- CORS: tarayıcı istemcileri (Next.js konsol) yalnız Gateway'e konuşur (docs/04 §3). İzinli
// kaynaklar config'ten gelir (Cors:AllowedOrigins). Kimlik bilgisi (JWT Authorization başlığı)
// taşınır; SignalR için gerekli. Üretimde origin listesi ortam değişkeninden sıkılaştırılır. ---
const string CorsPolicy = "halos-clients";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            // Kaynak verilmezse geliştirmede engellememek için herhangi bir origin'e izin ver
            // (kimlik bilgisiz). Üretimde Cors:AllowedOrigins MUTLAKA ayarlanır.
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// --- YARP ters-vekil: rotalar/kümeler appsettings "ReverseProxy" bölümünden yüklenir.
// Rota→servis eşlemesi ve destinasyonlar (docker servis adı veya localhost) config'te; ortam
// değişkeniyle override edilebilir (ReverseProxy__Clusters__sales__Destinations__primary__Address). ---
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseCors(CorsPolicy);

// Sağlık ucu (yönlendirme yok) — orkestrasyon/health-check için.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }));

// Tüm eşleşen istekleri arka servislere yönlendir. CORS politikası proxy rotalarına da uygulanır.
app.MapReverseProxy();

app.Run();

/// <summary>Integration testleri (WebApplicationFactory) için erişilebilir giriş noktası.</summary>
public partial class Program;

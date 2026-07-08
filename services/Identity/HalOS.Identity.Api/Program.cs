using System.Text;
using HalOS.BuildingBlocks.Application;
using HalOS.Identity.Api;
using HalOS.Identity.Api.Authentication;
using HalOS.Identity.Api.Authorization;
using HalOS.Identity.Application;
using HalOS.Identity.Application.Abstractions;
using HalOS.Identity.Infrastructure;
using HalOS.Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog: yapısal loglama → konsol + (yapılandırılmışsa) Seq (docs/04 §8).
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// --- Uygulama katmanları (Clean Architecture kompozisyon kökü) ---
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// Tenant ve kullanıcı bağlamı JWT claim'lerinden çözülür (docs/04 §7, docs/07 §6).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
// Denetim (audit_log) "kim" bilgisi: mevcut kullanıcı bağlamını saran paylaşılan IAuditActor
// adaptörü (mevcut ICurrentUserContext TAŞINMAZ/DEĞİŞTİRİLMEZ) (docs/05 §3.11).
builder.Services.AddScoped<IAuditActor, CurrentUserAuditActor>();

// --- Kimlik doğrulama: JWT Bearer (docs/04 ADR-009) ---
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

// İmzalama anahtarı config/ortam değişkeninden gelir (üretimde Vault, docs/04 §7, docs/07 §güvenlik).
// Development DIŞINDA boş/kısa anahtar KABUL EDİLMEZ → fail-fast (zayıf sabit anahtara asla düşme).
// Development'ta anahtar yoksa geçici bir tane üretilip log'lanır (yalnızca dev kolaylığı).
var signingKey = JwtSigningKeyResolver.Resolve(jwtOptions.SigningKey, builder.Environment.IsDevelopment());

// Çözümlenen anahtar tek kaynak: hem imzalama (TokenService → IOptions<JwtOptions>) hem
// doğrulama (JwtBearer) aynı anahtarı kullansın diye options'a geri yazılır.
builder.Services.PostConfigure<JwtOptions>(o => o.SigningKey = signingKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            RoleClaimType = HalOSClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// --- Yetkilendirme: RBAC rol politikaları (docs/03 §3) ---
builder.Services.AddAuthorization(options => options.AddHalOSPolicies());

// --- OpenTelemetry izleme (docs/04 §8) ---
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("HalOS.Identity"))
    .WithTracing(tracing =>
    {
        // ASP.NET Core ve HttpClient iz'leri toplanır. Dışa aktarıcı (OTLP → Collector)
        // sonraki fazda eklenecek; şu an iz'ler yalnızca in-process akıtılır (docs/04 §8).
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

// --- Global hata yönetimi: beklenmeyen istisnalar ProblemDetails ile net döner (docs/07 §10) ---
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Beklenmeyen istisnaları yakalar ve RFC 7807 ProblemDetails'e çevirir (docs/07 §10).
app.UseExceptionHandler();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Integration testleri (WebApplicationFactory) için erişilebilir giriş noktası.</summary>
public partial class Program;

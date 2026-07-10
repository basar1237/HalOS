using System.Text;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Infrastructure.Messaging;
using HalOS.ColdChain.Api;
using HalOS.ColdChain.Api.Authentication;
using HalOS.ColdChain.Api.Authorization;
using HalOS.ColdChain.Application;
using HalOS.ColdChain.Application.Abstractions;
using HalOS.ColdChain.Infrastructure;
using HalOS.ColdChain.Infrastructure.Persistence;
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
builder.Services.AddColdChainApplication(builder.Configuration);
builder.Services.AddColdChainInfrastructure(builder.Configuration);

// Tenant + kullanıcı bağlamı JWT claim'lerinden çözülür (docs/04 §7, docs/07 §6 / BK-8). ColdChain
// yalnız yayıncıdır (consumer yok) ama outbox dispatcher publish sırasında tenant taşımaz gerektirmez;
// yine de ITenantContext HTTP isteğinden çözülür (okuma/yazma tenant filtresi için).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

// Mesajlaşma (docs/04 ADR-006 / §10): ColdChain YALNIZ yayıncıdır — TemperatureThresholdBreached
// event'ini el-yapımı outbox üzerinden yayınlar (OutboxDispatcher<ColdChainDbContext>). Consumer yok.
builder.Services.AddHalOSMessaging<ColdChainDbContext>(builder.Configuration);

// --- Kimlik doğrulama: JWT Bearer (docs/04 ADR-009); token'ları Identity servisi üretir ---
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

// Development DIŞINDA eksik/zayıf anahtar KABUL EDİLMEZ → fail-fast (docs/07 §güvenlik).
var signingKey = JwtSigningKeyResolver.Resolve(jwtOptions.SigningKey, builder.Environment.IsDevelopment());

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
builder.Services.AddAuthorization(options => options.AddColdChainPolicies());

// --- OpenTelemetry izleme (docs/04 §8) ---
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("HalOS.ColdChain"))
    .WithTracing(tracing =>
    {
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

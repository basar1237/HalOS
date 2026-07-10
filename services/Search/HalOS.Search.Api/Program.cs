using System.Text;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Infrastructure.Messaging;
using HalOS.Search.Api.Authentication;
using HalOS.Search.Api.Authorization;
using HalOS.Search.Application;
using HalOS.Search.Application.Consumers;
using HalOS.Search.Infrastructure;
using MassTransit;
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
// Application: SearchQueryHandler. Infrastructure: ISearchIndex (config'e göre Elasticsearch/InMemory).
builder.Services.AddSearchApplication();
builder.Services.AddSearchInfrastructure(builder.Configuration);

// Tenant bağlamı: HTTP isteğinde JWT tenant claim'inden (arama filtresi, BK-8); consumer scope'unda
// ise mesajdan doldurulan AmbientTenantContext devreye girer (docs/07 §6). Composite ile ikisi
// birleşir. AmbientTenantContext'i TenantConsumeFilter mesajdan set eder.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpTenantContext>();
builder.Services.AddScoped<AmbientTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp =>
    new CompositeTenantContext(
        sp.GetRequiredService<HttpTenantContext>(),
        sp.GetRequiredService<AmbientTenantContext>()));

// --- Mesajlaşma (docs/04 ADR-006 / §10) ---
// DİKKAT: Search'ün DbContext'i YOK → AddHalOSMessaging<TContext> KULLANILMAZ (o, el-yapımı outbox'ı
// yayınlayan OutboxDispatcher<TContext> ister). Search salt tüketici/indeksleyicidir, event YAYMAZ;
// bu yüzden MassTransit doğrudan kurulur: iki consumer + RabbitMQ + TenantConsumeFilter (tenant
// mesajdan → AmbientTenantContext, BK-8). OutboxDispatcher/IEventPublisher EKLENMEZ.
var rabbit = builder.Configuration.GetSection(RabbitMqOptions.SectionName)
    .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PartyRegisteredConsumer>();
    x.AddConsumer<SaleCompletedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbit.Host, rabbit.VirtualHost, h =>
        {
            h.Username(rabbit.Username);
            h.Password(rabbit.Password);
        });

        // Gelen her mesajda tenant'ı event'ten (ITenantScopedEvent) çözüp ambient bağlama yaz —
        // indekslenen dokümanın doğru tenant'a bağlanmasını sağlar (docs/07 §6 / BK-8).
        cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);

        cfg.ConfigureEndpoints(context);
    });
});

// --- Kimlik doğrulama: JWT Bearer (docs/04 ADR-009); token'ları Identity servisi üretir ---
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

// Development DIŞINDA eksik/zayıf anahtar KABUL EDİLMEZ → fail-fast (docs/07 §güvenlik).
var signingKey = JwtSigningKeyResolver.Resolve(jwtOptions.SigningKey, builder.Environment.IsDevelopment());

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // JWT kısa claim adlarını (ör. "role") uzun URI'ye EŞLEME (MapInboundClaims=true varsayılanı)
        // RoleClaimType="role" ile IsInRole'u bozar (403). Kapat → custom claimler korunur.
        options.MapInboundClaims = false;
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
builder.Services.AddAuthorization(options => options.AddSearchPolicies());

// --- OpenTelemetry izleme (docs/04 §8) ---
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("HalOS.Search"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Gerçek ES kullanılıyorsa arama indeksi/mapping'i başlangıçta idempotent oluştur (docs/06 S2.3).
// InMemory'de no-op. ES erişilemezse içeride log'lanır, uygulama yine ayağa kalkar.
await app.Services.EnsureSearchIndexAsync();

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

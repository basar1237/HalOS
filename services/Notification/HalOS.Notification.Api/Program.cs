using System.Text;
using HalOS.BuildingBlocks.Infrastructure.Messaging;
using HalOS.Notification.Api.Authentication;
using HalOS.Notification.Api.Realtime;
using HalOS.Notification.Application;
using HalOS.Notification.Application.Abstractions;
using HalOS.Notification.Application.Consumers;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// .NET 8 JsonWebTokenHandler, JwtBearerOptions.MapInboundClaims=false'u handler instance'ına yansıtmaz
// (statik DefaultMapInboundClaims=true kullanır) → "role" claim'i URI'ye map'lenir, RoleClaimType="role"
// ile IsInRole BOZULUR (403). Statik varsayılanı kapatıp kısa claim adlarını KORU (RBAC düzeltmesi).
Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultMapInboundClaims = false;

// Serilog: yapısal loglama → konsol + (yapılandırılmışsa) Seq (docs/04 §8).
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// --- Uygulama katmanları (Clean Architecture kompozisyon kökü) ---
builder.Services.AddNotificationApplication();

// Canlı yayın soyutlaması → SignalR uygulaması. Consumer IDashboardBroadcaster'ı çağırır; somut
// tip IHubContext<DashboardHub> ile YALNIZ ilgili tenant grubuna yayınlar (BK-8, docs/06 S2.2).
builder.Services.AddSingleton<IDashboardBroadcaster, SignalRDashboardBroadcaster>();

// --- Realtime: SignalR (Microsoft.AspNetCore.SignalR, ASP.NET Core built-in, KARAR: STACK) ---
builder.Services.AddSignalR();

// Tenant bağlamı: bu servis HTTP sorgusu SUNMAZ (salt tüketici→broadcast). Consumer scope'unda
// TenantConsumeFilter mesajın tenant'ını AmbientTenantContext'e yazar (docs/07 §6 / BK-8). Yayın
// hedefi zaten event'in TenantId'sinden alınır; ambient bağlam filtre sözleşmesi için kaydedilir.
builder.Services.AddScoped<AmbientTenantContext>();

// --- Mesajlaşma (docs/04 ADR-006 / §10) ---
// DİKKAT: Notification'ın DbContext'i YOK → AddHalOSMessaging<TContext> KULLANILMAZ (o el-yapımı
// outbox'ı yayınlayan OutboxDispatcher<TContext> ister). Notification salt tüketici→broadcast'tir,
// event YAYMAZ; bu yüzden MassTransit DOĞRUDAN kurulur (Search deseni): SaleCompletedConsumer +
// RabbitMQ + TenantConsumeFilter (tenant mesajdan → AmbientTenantContext, BK-8). OutboxDispatcher YOK.
var rabbit = builder.Configuration.GetSection(RabbitMqOptions.SectionName)
    .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SaleCompletedConsumer>();
    x.AddConsumer<TemperatureThresholdBreachedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbit.Host, rabbit.VirtualHost, h =>
        {
            h.Username(rabbit.Username);
            h.Password(rabbit.Password);
        });

        // Gelen her mesajda tenant'ı event'ten (ITenantScopedEvent) çözüp ambient bağlama yaz (BK-8).
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

        // SignalR STANDARDI: WebSocket el sıkışmasında tarayıcı Authorization başlığı gönderemez;
        // token query string'de "access_token" olarak taşınır. Hub yoluna gelen isteklerde token'ı
        // query'den al (docs/06 S2.2). Diğer uçlar normal Bearer başlığını kullanır.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/dashboard"))
                {
                    ctx.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// --- OpenTelemetry izleme (docs/04 §8) ---
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("HalOS.Notification"))
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

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Canlı dashboard hub'ı: patron/yönetici istemcisi buraya bağlanır (docs/06 S2.2).
app.MapHub<DashboardHub>("/hubs/dashboard");

app.Run();

/// <summary>Integration testleri (WebApplicationFactory) için erişilebilir giriş noktası.</summary>
public partial class Program;

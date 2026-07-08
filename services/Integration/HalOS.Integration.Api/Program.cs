using System.Text;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Infrastructure.Messaging;
using HalOS.Integration.Api;
using HalOS.Integration.Api.Authentication;
using HalOS.Integration.Api.Authorization;
using HalOS.Integration.Application;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Consumers;
using HalOS.Integration.Infrastructure;
using HalOS.Integration.Infrastructure.Persistence;
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
builder.Services.AddIntegrationApplication(builder.Configuration);
builder.Services.AddIntegrationInfrastructure(builder.Configuration);

// Tenant + kullanıcı bağlamı JWT claim'lerinden çözülür (docs/04 §7, docs/07 §6). Integration
// SALT consumer'dır (SaleCompleted + ProducerWithholdingProfileChanged); ITenantContext
// CompositeTenantContext'e bağlanır: HTTP isteğinde birincil (JWT) kaynak, broker consumer
// scope'unda ise mesajdan doldurulan AmbientTenantContext devreye girer (docs/07 §6 / BK-8).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp =>
    new CompositeTenantContext(
        sp.GetRequiredService<HttpTenantContext>(),
        sp.GetRequiredService<AmbientTenantContext>()));
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

// Mesajlaşma (docs/04 ADR-006/ADR-007 / §10): Integration TÜKETİCİdir + kendi event'lerini
// (ProducerReceiptIssued) el-yapımı outbox üzerinden yayınlar (dispatcher). SaleCompletedConsumer,
// kayıt tutmayan müstahsil için e-MM üretir; ProducerWithholdingProfileChangedConsumer müstahsil
// vergi/kayıt profilini senkronlar. Tenant TenantConsumeFilter ile mesajdan çözülür (BK-8).
// MassTransit'in kendi outbox'ı kapalı.
builder.Services.AddHalOSMessaging<IntegrationDbContext>(
    builder.Configuration,
    x =>
    {
        x.AddConsumer<SaleCompletedConsumer>();
        x.AddConsumer<ProducerWithholdingProfileChangedConsumer>();
        x.AddConsumer<ConsignmentReceivedConsumer>();
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
builder.Services.AddAuthorization(options => options.AddIntegrationPolicies());

// --- OpenTelemetry izleme (docs/04 §8) ---
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("HalOS.Integration"))
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

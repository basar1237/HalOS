using HalOS.BuildingBlocks.Contracts;
using HalOS.Sales.Domain.ReadModels;
using HalOS.Sales.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HalOS.Sales.Infrastructure.Messaging;

/// <summary>
/// Party servisinden gelen <see cref="ProducerWithholdingProfileChanged"/>'i tüketip Sales'in
/// <see cref="ProducerRateProfile"/> okuma modelini UPSERT eder (docs/02 §6, hakediş doğruluğu;
/// docs/04 §10 event-taşımalı entegrasyon). Böylece satış tamamlanırken IRateProvider oranları
/// tenant config yerine müstahsilin gerçek profilinden çözer.
///
/// Tenant bağlamı, gelen mesajdan <see cref="TenantConsumeFilter{T}"/> ile ambient tenant'a
/// set edilmiştir (mesaj <see cref="ITenantScopedEvent"/>); dolayısıyla <see cref="SalesDbContext"/>
/// global query filter'ı DOĞRU tenant'ta çalışır ve upsert o tenant kapsamında izole kalır
/// (docs/07 §6 / BK-8). El-yapımı outbox korunur; bu consumer yalnız okuma modelini günceller.
/// </summary>
public sealed class ProducerWithholdingProfileChangedConsumer : IConsumer<ProducerWithholdingProfileChanged>
{
    private readonly SalesDbContext _dbContext;
    private readonly ILogger<ProducerWithholdingProfileChangedConsumer> _logger;

    public ProducerWithholdingProfileChangedConsumer(
        SalesDbContext dbContext,
        ILogger<ProducerWithholdingProfileChangedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProducerWithholdingProfileChanged> context)
    {
        var message = context.Message;

        // Tenant filtresi ambient tenant'tan çalışır → tenant içinde müstahsili ara (upsert).
        var existing = await _dbContext.ProducerRateProfiles
            .FirstOrDefaultAsync(
                p => p.ProducerPartyId == message.ProducerPartyId,
                context.CancellationToken);

        if (existing is null)
        {
            _dbContext.ProducerRateProfiles.Add(ProducerRateProfile.Create(
                message.TenantId,
                message.ProducerPartyId,
                message.AgriWithholdingRate,
                message.FarmerSskRate,
                message.OccurredOnUtc));
        }
        else
        {
            existing.Apply(
                message.AgriWithholdingRate,
                message.FarmerSskRate,
                message.OccurredOnUtc);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Müstahsil oran profili senkronlandı: Tenant={TenantId} Producer={ProducerPartyId}.",
            message.TenantId,
            message.ProducerPartyId);
    }
}

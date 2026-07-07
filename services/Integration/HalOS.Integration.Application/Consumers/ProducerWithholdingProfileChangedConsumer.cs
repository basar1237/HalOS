using HalOS.BuildingBlocks.Contracts;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Domain.ReadModels;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Integration.Application.Consumers;

/// <summary>
/// Party servisinden gelen <see cref="ProducerWithholdingProfileChanged"/>'i tüketip Integration'ın
/// <see cref="ProducerTaxProfile"/> okuma modelini UPSERT eder (docs/02 §6; docs/04 §10 event-taşımalı
/// entegrasyon). e-MM üretim kararı için müstahsilin <see cref="ProducerWithholdingProfileChanged.KeepsRecords"/>
/// bilgisi bu modelde tutulur (BK-4). Sales'in aynı adlı consumer'ıyla birebir desen; buradaki fark:
/// KeepsRecords de saklanır (Integration e-MM kararı için ona bakar; Sales oran senkronu için bakmaz).
///
/// Tenant bağlamı, gelen mesajdan <see cref="TenantConsumeFilter{T}"/> ile ambient tenant'a set edilmiştir
/// (mesaj <see cref="ITenantScopedEvent"/>); dolayısıyla DbContext global query filter'ı DOĞRU tenant'ta
/// çalışır ve upsert o tenant kapsamında izole kalır (docs/07 §6 / BK-8). El-yapımı outbox korunur; bu
/// consumer yalnız okuma modelini günceller. Sıra-dışı teslimat için monoton guard (ProducerTaxProfile.Apply).
/// </summary>
public sealed class ProducerWithholdingProfileChangedConsumer : IConsumer<ProducerWithholdingProfileChanged>
{
    private readonly IProducerTaxProfileWriter _profiles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProducerWithholdingProfileChangedConsumer> _logger;

    public ProducerWithholdingProfileChangedConsumer(
        IProducerTaxProfileWriter profiles,
        IUnitOfWork unitOfWork,
        ILogger<ProducerWithholdingProfileChangedConsumer> logger)
    {
        _profiles = profiles;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProducerWithholdingProfileChanged> context)
    {
        var message = context.Message;

        // Tenant filtresi ambient tenant'tan çalışır → tenant içinde müstahsili ara (upsert).
        var existing = await _profiles.GetByProducerAsync(message.ProducerPartyId, context.CancellationToken);

        if (existing is null)
        {
            _profiles.Add(ProducerTaxProfile.Create(
                message.TenantId,
                message.ProducerPartyId,
                message.KeepsRecords,
                message.AgriWithholdingRate,
                message.FarmerSskRate,
                message.OccurredOnUtc));
        }
        else
        {
            existing.Apply(
                message.KeepsRecords,
                message.AgriWithholdingRate,
                message.FarmerSskRate,
                message.OccurredOnUtc);
        }

        await _unitOfWork.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Müstahsil vergi/kayıt profili senkronlandı: Tenant={TenantId} Producer={ProducerPartyId} KayıtTutuyor={KeepsRecords}.",
            message.TenantId,
            message.ProducerPartyId,
            message.KeepsRecords);
    }
}

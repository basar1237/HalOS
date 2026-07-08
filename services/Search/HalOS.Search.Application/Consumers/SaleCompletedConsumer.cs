using System.Globalization;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Search.Application.Abstractions;
using HalOS.Search.Domain.Documents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Search.Application.Consumers;

/// <summary>
/// Sales servisinden gelen <see cref="SaleCompleted"/>'ı tüketip <see cref="SaleSearchDocument"/>
/// olarak indeksler (docs/06 S2.3). Search salt tüketici/indeksleyicidir: yalnız arama deposuna
/// YAZAR, kaynak servisin (Sales) DB'sine DOKUNMAZ, event YAYMAZ (CQRS ayrı okuma modeli,
/// docs/04 ADR-007).
///
/// Tenant mesajdan gelir (<see cref="ITenantScopedEvent"/>) ve doküman <c>TenantId</c>'sine yazılır;
/// arama JWT tenant'ına göre filtreleneceğinden çapraz-tenant sızıntısı olmaz (BK-8). İndeksleme
/// idempotenttir (aynı <c>SaleTransactionId</c> üzerine upsert) — broker retry'da çift kayıt olmaz.
/// </summary>
public sealed class SaleCompletedConsumer : IConsumer<SaleCompleted>
{
    private readonly ISearchIndex _index;
    private readonly ILogger<SaleCompletedConsumer> _logger;

    public SaleCompletedConsumer(ISearchIndex index, ILogger<SaleCompletedConsumer> logger)
    {
        _index = index;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SaleCompleted> context)
    {
        var message = context.Message;

        var summary = string.Format(
            CultureInfo.InvariantCulture,
            "Satış {0:yyyy-MM-dd} — {1:0.00}",
            message.SoldAt,
            message.GrossAmount);

        var document = new SaleSearchDocument
        {
            Id = SaleSearchDocument.MakeId(message.SaleTransactionId),
            TenantId = message.TenantId,
            Type = SearchDocumentType.Sale,
            Summary = summary,
            SaleTransactionId = message.SaleTransactionId,
            BuyerPartyId = message.BuyerPartyId,
            ProducerPartyId = message.ProducerPartyId,
            GrossAmount = message.GrossAmount,
            SoldAt = message.SoldAt
        };

        await _index.IndexAsync(document, context.CancellationToken);

        _logger.LogInformation(
            "Satış arama dokümanı indekslendi: Tenant={TenantId} Sale={SaleTransactionId}.",
            message.TenantId,
            message.SaleTransactionId);
    }
}

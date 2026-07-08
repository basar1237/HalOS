using HalOS.BuildingBlocks.Contracts;
using HalOS.Search.Application.Abstractions;
using HalOS.Search.Domain.Documents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HalOS.Search.Application.Consumers;

/// <summary>
/// Party servisinden gelen <see cref="PartyRegistered"/>'ı tüketip <see cref="PartySearchDocument"/>
/// olarak indeksler (docs/06 S2.3, "Ali'nin her şeyini 1 sn'de"). Search salt tüketici/indeksleyicidir:
/// yalnız arama deposuna YAZAR, kaynak servisin (Party) DB'sine DOKUNMAZ, event YAYMAZ (CQRS ayrı
/// okuma modeli, docs/04 ADR-007).
///
/// Tenant mesajdan gelir (<see cref="ITenantScopedEvent"/>) ve doküman <c>TenantId</c>'sine yazılır;
/// arama sonradan JWT tenant'ına göre filtreleneceğinden çapraz-tenant sızıntısı olmaz (BK-8).
/// İndeksleme idempotenttir (aynı <c>PartyId</c> üzerine upsert) — broker retry'da çift kayıt olmaz.
/// </summary>
public sealed class PartyRegisteredConsumer : IConsumer<PartyRegistered>
{
    private readonly ISearchIndex _index;
    private readonly ILogger<PartyRegisteredConsumer> _logger;

    public PartyRegisteredConsumer(ISearchIndex index, ILogger<PartyRegisteredConsumer> logger)
    {
        _index = index;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PartyRegistered> context)
    {
        var message = context.Message;

        var summary = string.IsNullOrWhiteSpace(message.TaxNumber)
            ? message.DisplayName
            : $"{message.DisplayName} ({message.TaxNumber})";

        var document = new PartySearchDocument
        {
            Id = PartySearchDocument.MakeId(message.PartyId),
            TenantId = message.TenantId,
            Type = SearchDocumentType.Party,
            Summary = summary,
            PartyId = message.PartyId,
            DisplayName = message.DisplayName,
            TaxNumber = message.TaxNumber,
            PartyType = message.PartyType
        };

        await _index.IndexAsync(document, context.CancellationToken);

        _logger.LogInformation(
            "Taraf arama dokümanı indekslendi: Tenant={TenantId} Party={PartyId}.",
            message.TenantId,
            message.PartyId);
    }
}

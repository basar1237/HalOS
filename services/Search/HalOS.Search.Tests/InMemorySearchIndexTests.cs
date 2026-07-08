using FluentAssertions;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Search.Application.Consumers;
using HalOS.Search.Application.Search;
using HalOS.Search.Domain.Documents;
using HalOS.Search.Infrastructure.Search;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HalOS.Search.Tests;

/// <summary>
/// Search servisi çekirdek davranışı, gerçek <see cref="InMemorySearchIndex"/> ile (ES YOK — KARAR:
/// STACK, servis ES olmadan da test edilir). Kapsam (docs/06 S2.3):
/// <list type="bullet">
///   <item>PartyRegistered tüketimi party'yi indeksler; arama ada/kimlik no'ya göre bulur.</item>
///   <item>SaleCompleted tüketimi satışı indeksler; arama bulur.</item>
///   <item>ÇAPRAZ-TENANT araması sonuç DÖNMEZ (BK-8, sızıntı YASAK).</item>
///   <item>Tür filtresi (type=Party/Sale) yalnız o türü döner.</item>
///   <item>İdempotency: aynı event iki kez tüketilse tek doküman kalır.</item>
/// </list>
/// </summary>
public sealed class InMemorySearchIndexTests
{
    private static ConsumeContext<T> ContextFor<T>(T message)
        where T : class
    {
        var mock = new Mock<ConsumeContext<T>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    private static PartyRegistered SampleParty(Guid tenantId, Guid partyId, string name, string? taxNumber, string type = "Buyer") =>
        new(partyId, tenantId, name, taxNumber, type, DateTime.UtcNow);

    private static SaleCompleted SampleSale(Guid tenantId, Guid saleId) =>
        new(
            SaleTransactionId: saleId,
            TenantId: tenantId,
            BuyerPartyId: Guid.NewGuid(),
            ProducerPartyId: Guid.NewGuid(),
            SoldAt: new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc),
            GrossAmount: 1250.50m,
            CommissionAmount: 100m,
            CommissionVatAmount: 20m,
            AgriWithholdingAmount: 25m,
            FarmerSskAmount: 12.5m,
            MarketFeeAmount: 12.5m,
            TotalDeductions: 150m,
            NetAmount: 1100.50m,
            SettlementDueDate: new DateTime(2026, 7, 28),
            Lines: Array.Empty<SaleCompletedLine>(),
            OccurredOnUtc: DateTime.UtcNow);

    [Fact]
    public async Task PartyRegistered_IsIndexed_And_FoundByName()
    {
        var index = new InMemorySearchIndex();
        var consumer = new PartyRegisteredConsumer(index, NullLogger<PartyRegisteredConsumer>.Instance);
        var handler = new SearchQueryHandler(index);
        var tenantId = Guid.NewGuid();

        await consumer.Consume(ContextFor(SampleParty(tenantId, Guid.NewGuid(), "Manav Ali", "1234567890")));

        var result = await handler.HandleAsync(tenantId, new SearchQuery("Ali", null, 20));

        result.Hits.Should().ContainSingle();
        result.Hits[0].Type.Should().Be(SearchDocumentType.Party);
        result.Hits[0].Summary.Should().Contain("Manav Ali");
    }

    [Fact]
    public async Task PartyRegistered_IsFoundByTaxNumber()
    {
        var index = new InMemorySearchIndex();
        var consumer = new PartyRegisteredConsumer(index, NullLogger<PartyRegisteredConsumer>.Instance);
        var handler = new SearchQueryHandler(index);
        var tenantId = Guid.NewGuid();

        await consumer.Consume(ContextFor(SampleParty(tenantId, Guid.NewGuid(), "Manav Ali", "9876543210")));

        var result = await handler.HandleAsync(tenantId, new SearchQuery("9876543210", null, 20));

        result.Hits.Should().ContainSingle();
    }

    [Fact]
    public async Task SaleCompleted_IsIndexed_And_Found()
    {
        var index = new InMemorySearchIndex();
        var consumer = new SaleCompletedConsumer(index, NullLogger<SaleCompletedConsumer>.Instance);
        var handler = new SearchQueryHandler(index);
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        await consumer.Consume(ContextFor(SampleSale(tenantId, saleId)));

        // Satış Id'sinin "N" formatı searchable_text içinde; ona göre bulunmalı.
        var result = await handler.HandleAsync(tenantId, new SearchQuery(saleId.ToString("N"), null, 20));

        result.Hits.Should().ContainSingle();
        result.Hits[0].Type.Should().Be(SearchDocumentType.Sale);
    }

    [Fact]
    public async Task Search_DoesNotLeakAcrossTenants_BK8()
    {
        // BK-8: tenant A'nın dokümanı tenant B aramasında ASLA görünmez (çapraz-tenant sızıntısı YASAK).
        var index = new InMemorySearchIndex();
        var consumer = new PartyRegisteredConsumer(index, NullLogger<PartyRegisteredConsumer>.Instance);
        var handler = new SearchQueryHandler(index);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await consumer.Consume(ContextFor(SampleParty(tenantA, Guid.NewGuid(), "Gizli Ali", "1112223334")));

        // Tenant B aynı sorguyu yapsa da hiçbir şey görmemeli.
        var leaked = await handler.HandleAsync(tenantB, new SearchQuery("Ali", null, 20));
        leaked.Hits.Should().BeEmpty();

        // Tenant A ise kendi dokümanını görmeli (kontrol).
        var own = await handler.HandleAsync(tenantA, new SearchQuery("Ali", null, 20));
        own.Hits.Should().ContainSingle();
    }

    [Fact]
    public async Task Search_TypeFilter_ReturnsOnlyRequestedType()
    {
        var index = new InMemorySearchIndex();
        var partyConsumer = new PartyRegisteredConsumer(index, NullLogger<PartyRegisteredConsumer>.Instance);
        var saleConsumer = new SaleCompletedConsumer(index, NullLogger<SaleCompletedConsumer>.Instance);
        var handler = new SearchQueryHandler(index);
        var tenantId = Guid.NewGuid();
        var saleId = Guid.NewGuid();

        // Aynı tenant'ta hem party (adında "veli") hem satış indeksle. Ortak eşleşen bir terim
        // ("N" GUID) yerine, iki türü de kapsayan ayrı sorgular + tür filtresi test edilir.
        await partyConsumer.Consume(ContextFor(SampleParty(tenantId, Guid.NewGuid(), "Mustahsil Veli", "5556667778")));
        await saleConsumer.Consume(ContextFor(SampleSale(tenantId, saleId)));

        // Party türü filtresi: satış Id sorgusu bile Party filtresiyle satışı DÖNDÜRMEZ.
        var onlyParty = await handler.HandleAsync(tenantId, new SearchQuery(saleId.ToString("N"), SearchDocumentType.Party, 20));
        onlyParty.Hits.Should().BeEmpty();

        // Sale türü filtresi: satış Id ile arama yalnız satışı döner.
        var onlySale = await handler.HandleAsync(tenantId, new SearchQuery(saleId.ToString("N"), SearchDocumentType.Sale, 20));
        onlySale.Hits.Should().ContainSingle().Which.Type.Should().Be(SearchDocumentType.Sale);
    }

    [Theory]
    [InlineData("party", SearchDocumentType.Party)]
    [InlineData("PARTY", SearchDocumentType.Party)]
    [InlineData("  Party  ", SearchDocumentType.Party)]
    [InlineData("sale", SearchDocumentType.Sale)]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    public void TryNormalize_KnownOrEmptyType_ReturnsCanonical(string? input, string? expected)
    {
        // REGRESYON (adversarial review — major): ham istemci girdisi (ör. küçük harf ?type=party)
        // TEK noktada kanonikleştirilmeli; böylece InMemory (case-insensitive) ile ES (keyword term,
        // case-sensitive) AYNI kanonik değeri alır ve iki backend AYNI sonucu verir.
        SearchDocumentType.TryNormalize(input, out var canonical).Should().BeTrue();
        canonical.Should().Be(expected);
    }

    [Theory]
    [InlineData("Belge")]
    [InlineData("unknown")]
    [InlineData("Part")]
    public void TryNormalize_UnknownType_Fails(string input)
    {
        // Bilinmeyen tür sessizce yok sayılmaz — çağıran (API) 400 döndürebilsin diye false döner.
        SearchDocumentType.TryNormalize(input, out var canonical).Should().BeFalse();
        canonical.Should().BeNull();
    }

    [Fact]
    public async Task Search_LowercaseTypeFilter_AfterNormalization_MatchesCanonical()
    {
        // REGRESYON: istemci ?type=party (küçük harf) gönderdiğinde, kanonikleştirme sonrası
        // InMemory party dokümanını DÖNER — normalizasyon olmadan ES'te boş dönerdi (davranış uyuşmazlığı).
        var index = new InMemorySearchIndex();
        var partyConsumer = new PartyRegisteredConsumer(index, NullLogger<PartyRegisteredConsumer>.Instance);
        var saleConsumer = new SaleCompletedConsumer(index, NullLogger<SaleCompletedConsumer>.Instance);
        var handler = new SearchQueryHandler(index);
        var tenantId = Guid.NewGuid();

        await partyConsumer.Consume(ContextFor(SampleParty(tenantId, Guid.NewGuid(), "Manav Ali", "1234567890")));
        await saleConsumer.Consume(ContextFor(SampleSale(tenantId, Guid.NewGuid())));

        SearchDocumentType.TryNormalize("party", out var canonicalType).Should().BeTrue();
        var result = await handler.HandleAsync(tenantId, new SearchQuery("Ali", canonicalType, 20));

        result.Hits.Should().ContainSingle().Which.Type.Should().Be(SearchDocumentType.Party);
    }

    [Fact]
    public async Task Consume_SamePartyTwice_IsIdempotent_NoDuplicate()
    {
        var index = new InMemorySearchIndex();
        var consumer = new PartyRegisteredConsumer(index, NullLogger<PartyRegisteredConsumer>.Instance);
        var handler = new SearchQueryHandler(index);
        var tenantId = Guid.NewGuid();
        var partyId = Guid.NewGuid();
        var message = SampleParty(tenantId, partyId, "Tekrar Ali", "0001112223");

        await consumer.Consume(ContextFor(message));
        await consumer.Consume(ContextFor(message)); // broker retry

        var result = await handler.HandleAsync(tenantId, new SearchQuery("Ali", null, 20));
        result.Hits.Should().ContainSingle(); // çift doküman YOK
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsEmpty()
    {
        var index = new InMemorySearchIndex();
        var consumer = new PartyRegisteredConsumer(index, NullLogger<PartyRegisteredConsumer>.Instance);
        var handler = new SearchQueryHandler(index);
        var tenantId = Guid.NewGuid();

        await consumer.Consume(ContextFor(SampleParty(tenantId, Guid.NewGuid(), "Bos Sorgu", null)));

        var result = await handler.HandleAsync(tenantId, new SearchQuery("   ", null, 20));
        result.Hits.Should().BeEmpty();
    }
}

using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Contracts;
using HalOS.Finance.Application.Consumers;
using HalOS.Finance.Domain.Enums;
using HalOS.Finance.Infrastructure.Persistence;
using HalOS.Finance.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HalOS.Finance.Tests.Consumers;

/// <summary>
/// SaleCompletedConsumer testleri (docs/02 §5/§6: SaleCompleted → Finans cari). Gerçek
/// FinanceDbContext (EF Core InMemory) + gerçek CurrentAccountRepository ile uçtan uca doğrular:
/// <list type="bullet">
///   <item>Alıcı carisine BORÇ = brüt; müstahsil carisine ALACAK = net hakediş; vade tarihi
///     mesajdan (SettlementDueDate) hareketin üzerine yazılır (BK-3).</item>
///   <item>Idempotency: aynı SaleTransactionId iki kez tüketilse çift kayıt oluşmaz (docs/04 §5).</item>
///   <item>Cari yoksa açılır (upsert); tenant testte DbContext bağlamına doğrudan verilir
///     (çalışma zamanında TenantConsumeFilter mesajdan doldurur — BK-8).</item>
/// </list>
/// </summary>
public sealed class SaleCompletedConsumerTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static FinanceDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new FinanceDbContext(options, tenantContext);
    }

    private static ConsumeContext<SaleCompleted> ContextFor(SaleCompleted message)
    {
        var mock = new Mock<ConsumeContext<SaleCompleted>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock.Object;
    }

    private static SaleCompleted SampleSale(Guid tenantId, Guid buyerId, Guid producerId, Guid saleId) =>
        new(
            SaleTransactionId: saleId,
            TenantId: tenantId,
            BuyerPartyId: buyerId,
            ProducerPartyId: producerId,
            SoldAt: new DateTime(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc),
            GrossAmount: 100.00m,
            CommissionAmount: 8.00m,
            CommissionVatAmount: 1.60m,
            AgriWithholdingAmount: 2.00m,
            FarmerSskAmount: 1.00m,
            MarketFeeAmount: 1.00m,
            TotalDeductions: 12.00m,
            NetAmount: 88.00m,
            SettlementDueDate: new DateTime(2026, 7, 28),
            // Tek kalem: 100 kg × 1,00 TL = 100,00 brüt (tutarlı örnek). Finance consumer'ı
            // Lines'ı kullanmaz; yalnız derlensin diye doldurulur.
            Lines: new[] { new SaleCompletedLine(Guid.NewGuid(), Guid.NewGuid(), 100.000m, "Kilogram") },
            OccurredOnUtc: DateTime.UtcNow);

    [Fact]
    public async Task Consume_WritesBuyerDebit_AndProducerCredit_WithCorrectAmountsAndDueDate()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new SaleCompletedConsumer(
                new CurrentAccountRepository(ctx), ctx, NullLogger<SaleCompletedConsumer>.Instance);
            await consumer.Consume(ContextFor(SampleSale(tenantId, buyerId, producerId, saleId)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var buyer = await ctx.CurrentAccounts.Include(a => a.Entries)
                .FirstAsync(a => a.PartyId == buyerId);
            var producer = await ctx.CurrentAccounts.Include(a => a.Entries)
                .FirstAsync(a => a.PartyId == producerId);

            // Alıcı: BORÇ = brüt (100), bakiye +100.
            buyer.Entries.Should().ContainSingle();
            buyer.Entries.Single().Direction.Should().Be(EntryDirection.Debit);
            buyer.Entries.Single().Type.Should().Be(EntryType.Sale);
            buyer.Entries.Single().Amount.Should().Be(100.00m);
            buyer.Balance.Should().Be(100.00m);

            // Müstahsil: ALACAK = net (88), bakiye −88, vade = 2026-07-28 (mesajdan, BK-3).
            producer.Entries.Should().ContainSingle();
            producer.Entries.Single().Direction.Should().Be(EntryDirection.Credit);
            producer.Entries.Single().Type.Should().Be(EntryType.Settlement);
            producer.Entries.Single().Amount.Should().Be(88.00m);
            producer.Entries.Single().DueDate.Should().Be(new DateTime(2026, 7, 28));
            producer.Balance.Should().Be(-88.00m);
        }
    }

    [Fact]
    public async Task Consume_SameSaleTwice_IsIdempotent_NoDuplicateEntries()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var message = SampleSale(tenantId, buyerId, producerId, saleId);

        // İlk teslimat.
        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new SaleCompletedConsumer(
                new CurrentAccountRepository(ctx), ctx, NullLogger<SaleCompletedConsumer>.Instance);
            await consumer.Consume(ContextFor(message));
        }

        // İkinci teslimat (broker retry) — aynı satış tekrar.
        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new SaleCompletedConsumer(
                new CurrentAccountRepository(ctx), ctx, NullLogger<SaleCompletedConsumer>.Instance);
            await consumer.Consume(ContextFor(message));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var buyer = await ctx.CurrentAccounts.Include(a => a.Entries).FirstAsync(a => a.PartyId == buyerId);
            var producer = await ctx.CurrentAccounts.Include(a => a.Entries).FirstAsync(a => a.PartyId == producerId);

            buyer.Entries.Should().ContainSingle();  // çift kayıt YOK.
            producer.Entries.Should().ContainSingle();
            buyer.Balance.Should().Be(100.00m);
            producer.Balance.Should().Be(-88.00m);
        }
    }

    [Fact]
    public async Task Consume_NonPositiveGross_DoesNotCommitOneSidedEntry_AndThrows()
    {
        // Bozuk/kötücül SaleCompleted: brüt<=0 (borç reddedilir) ama net>0 (alacak yazılabilir).
        // Kısmi/tek-taraflı cari yazımı çift-kayıt değişmezini bozar (docs/02 §5). Consumer HİÇBİR
        // şeyi kalıcılaştırmamalı ve istisna fırlatmalı → MassTransit retry/error queue (docs/04 §10).
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        var corrupt = SampleSale(tenantId, buyerId, producerId, saleId) with
        {
            GrossAmount = 0m,   // borç reddi (NonPositiveAmount)
            NetAmount = 88.00m, // alacak geçerli görünür
        };

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new SaleCompletedConsumer(
                new CurrentAccountRepository(ctx), ctx, NullLogger<SaleCompletedConsumer>.Instance);

            var act = () => consumer.Consume(ContextFor(corrupt));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        // Yeni bir bağlamda doğrula: ne alıcı borcu ne de müstahsil alacağı kalıcılaşmış olmalı.
        await using (var ctx = CreateContext(stub, dbName))
        {
            (await ctx.CurrentAccounts.Include(a => a.Entries).ToListAsync())
                .SelectMany(a => a.Entries).Should().BeEmpty();
            (await ctx.OutboxMessages.ToListAsync()).Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Consume_NegativeNet_DoesNotCommitOneSidedEntry_AndThrows()
    {
        // Net<0 (RecordSettlementCredit NegativeNet ile reddedilir) ama brüt geçerli → tek-taraflı
        // alıcı borcu yazılıp müstahsil hakedişi düşerse mustahsil hakedişi kaybolur. Engellenmeli.
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        var corrupt = SampleSale(tenantId, buyerId, producerId, saleId) with
        {
            GrossAmount = 100.00m,
            NetAmount = -1.00m, // negatif net → hakediş reddi
        };

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new SaleCompletedConsumer(
                new CurrentAccountRepository(ctx), ctx, NullLogger<SaleCompletedConsumer>.Instance);

            var act = () => consumer.Consume(ContextFor(corrupt));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            (await ctx.CurrentAccounts.Include(a => a.Entries).ToListAsync())
                .SelectMany(a => a.Entries).Should().BeEmpty();
            (await ctx.OutboxMessages.ToListAsync()).Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Consume_BuyerEqualsProducer_UsesSingleAccount_NoDuplicateOpen()
    {
        // Kenar durum: alıcı == müstahsil. GetOrOpen iki ayrı hesap açmamalı (unique tenant_id,
        // party_id ihlali); aynı hesaba hem BORÇ hem ALACAK yazılıp bakiye = brüt − net olmalı.
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var partyId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using (var ctx = CreateContext(stub, dbName))
        {
            var consumer = new SaleCompletedConsumer(
                new CurrentAccountRepository(ctx), ctx, NullLogger<SaleCompletedConsumer>.Instance);
            await consumer.Consume(ContextFor(SampleSale(tenantId, partyId, partyId, saleId)));
        }

        await using (var ctx = CreateContext(stub, dbName))
        {
            var accounts = await ctx.CurrentAccounts.Include(a => a.Entries)
                .Where(a => a.PartyId == partyId).ToListAsync();

            accounts.Should().ContainSingle(); // TEK hesap açıldı.
            var account = accounts.Single();
            account.Entries.Should().HaveCount(2); // BORÇ (satış) + ALACAK (hakediş).
            account.Balance.Should().Be(100.00m - 88.00m); // brüt − net = +12.
        }
    }

    [Fact]
    public async Task Consume_WritesPaymentDueToOutbox_WithTenant()
    {
        // docs/04 §10: hakediş kaydı PaymentDue event'ini tenant'lı olarak outbox'a atomik yazar.
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        var consumer = new SaleCompletedConsumer(
            new CurrentAccountRepository(ctx), ctx, NullLogger<SaleCompletedConsumer>.Instance);
        await consumer.Consume(ContextFor(SampleSale(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));

        var outbox = await ctx.OutboxMessages.ToListAsync();
        outbox.Should().Contain(m => m.Type.Contains("PaymentDue"));
        outbox.First(m => m.Type.Contains("PaymentDue")).TenantId.Should().Be(tenantId);
    }
}

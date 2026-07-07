using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.BuildingBlocks.Contracts;
using HalOS.BuildingBlocks.Domain;
using HalOS.Integration.Application.Abstractions;
using HalOS.Integration.Application.Consumers;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Domain.Enums;
using HalOS.Integration.Domain.ReadModels;
using HalOS.Integration.Infrastructure.Persistence;
using HalOS.Integration.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HalOS.Integration.Tests.Consumers;

/// <summary>
/// SaleCompletedConsumer testleri (docs/02 §3.5/§6: SaleCompleted → e-MM; docs/03 BK-4). Gerçek
/// IntegrationDbContext (EF Core InMemory) + gerçek ProducerReceiptRepository/ProducerTaxProfileReader
/// ile uçtan uca doğrular:
/// <list type="bullet">
///   <item>Kayıt TUTMAYAN müstahsil (KeepsRecords=false) → e-MM üretilir; net = brüt − stopaj − Bağ-Kur;
///     tutarlar SaleCompleted'tan (AgriWithholdingAmount/FarmerSskAmount) gelir.</item>
///   <item>Kayıt TUTAN müstahsil (KeepsRecords=true) → e-MM ÜRETİLMEZ (BK-4).</item>
///   <item>Profil yok → İSTİSNA (retry/error queue); e-MM üretilmez, yasal belge sessizce kaybolmaz.</item>
///   <item>Idempotency: aynı SaleTransactionId iki kez → tek e-MM (docs/04 §5).</item>
/// </list>
/// </summary>
public sealed class SaleCompletedConsumerTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private sealed class FakeGateway : IEDocumentGateway
    {
        public int SendCount { get; private set; }
        public int InvoiceCount { get; private set; }
        public int HksCount { get; private set; }

        public Task<Result<string>> SendProducerReceiptAsync(ProducerReceipt receipt, CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult<Result<string>>($"EMM-TEST-{SendCount:D4}");
        }

        public Task<Result<string>> SendInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            InvoiceCount++;
            return Task.FromResult<Result<string>>($"EFA-TEST-{InvoiceCount:D4}");
        }

        public Task<Result<string>> SendHksNotificationAsync(HksNotification notification, CancellationToken cancellationToken = default)
        {
            HksCount++;
            return Task.FromResult<Result<string>>($"HKS-TEST-{HksCount:D4}");
        }
    }

    private static SaleCompletedConsumer NewConsumer(IntegrationDbContext ctx, FakeGateway gateway) =>
        new(
            new ProducerReceiptRepository(ctx),
            new InvoiceRepository(ctx),
            new HksNotificationRepository(ctx),
            new ProducerTaxProfileReader(ctx),
            gateway,
            ctx,
            NullLogger<SaleCompletedConsumer>.Instance);

    private static IntegrationDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new IntegrationDbContext(options, tenantContext);
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
            OccurredOnUtc: DateTime.UtcNow);

    private static void SeedProfile(IntegrationDbContext ctx, Guid tenantId, Guid producerId, bool keepsRecords)
    {
        ctx.ProducerTaxProfiles.Add(ProducerTaxProfile.Create(
            tenantId, producerId, keepsRecords, 0.02m, 0.01m, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));
        ctx.SaveChanges();
    }

    [Fact]
    public async Task Consume_NonRecordKeepingProducer_IssuesEmm_WithStopajAndSskOnly()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        SeedProfile(ctx, tenantId, producerId, keepsRecords: false);

        var gateway = new FakeGateway();
        var consumer = NewConsumer(ctx, gateway);

        await consumer.Consume(ContextFor(SampleSale(tenantId, Guid.NewGuid(), producerId, saleId)));

        var receipt = await ctx.ProducerReceipts.Include(r => r.Deductions)
            .IgnoreQueryFilters().SingleAsync();
        receipt.SaleTransactionId.Should().Be(saleId);
        receipt.GrossAmount.Should().Be(100.00m);
        receipt.AgriWithholdingAmount.Should().Be(2.00m);
        receipt.FarmerSskAmount.Should().Be(1.00m);
        receipt.NetPayable.Should().Be(97.00m); // komisyon/rüsum/KDV GİRMEZ
        receipt.Status.Should().Be(ProducerReceiptStatus.Issued);
        receipt.ReceiptNumber.Should().NotBeNullOrWhiteSpace();
        receipt.Deductions.Should().HaveCount(2);
        gateway.SendCount.Should().Be(1);

        // BK-4 değişmez: her satış → e-Fatura (HAL/KOMİSYON, alıcıya) + HKS bildirimi.
        var invoice = await ctx.Invoices.IgnoreQueryFilters().SingleAsync();
        invoice.SaleTransactionId.Should().Be(saleId);
        invoice.Scenario.Should().Be(InvoiceScenario.Hal);
        invoice.Type.Should().Be(InvoiceType.Commission);
        invoice.CommissionAmount.Should().Be(8.00m);
        invoice.CommissionVatAmount.Should().Be(1.60m);
        invoice.TotalAmount.Should().Be(9.60m); // komisyon 8,00 + KDV 1,60
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        gateway.InvoiceCount.Should().Be(1);

        var hks = await ctx.HksNotifications.IgnoreQueryFilters().SingleAsync();
        hks.SaleTransactionId.Should().Be(saleId);
        hks.GrossAmount.Should().Be(100.00m);
        hks.CommissionAmount.Should().Be(8.00m);
        hks.MarketFeeAmount.Should().Be(1.00m); // rüsum AYRI taşınır (docs/02 §7, BK-5)
        hks.Status.Should().Be(HksNotificationStatus.Notified);
        gateway.HksCount.Should().Be(1);
    }

    [Fact]
    public async Task Consume_RecordKeepingProducer_DoesNotIssueEmm()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        SeedProfile(ctx, tenantId, producerId, keepsRecords: true);

        var gateway = new FakeGateway();
        var consumer = NewConsumer(ctx, gateway);

        await consumer.Consume(ContextFor(SampleSale(tenantId, Guid.NewGuid(), producerId, Guid.NewGuid())));

        // e-MM üretilmez (kayıt tutan müstahsil)...
        (await ctx.ProducerReceipts.IgnoreQueryFilters().AnyAsync()).Should().BeFalse();
        gateway.SendCount.Should().Be(0);

        // ...ama e-Fatura + HKS HER satış için üretilir (BK-4; KeepsRecords'tan bağımsız).
        (await ctx.Invoices.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await ctx.HksNotifications.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        gateway.InvoiceCount.Should().Be(1);
        gateway.HksCount.Should().Be(1);
    }

    [Fact]
    public async Task Consume_ProfileNotSynced_Throws_AndDoesNotIssue()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        // Profil YOK — bilerek seed edilmedi.
        var gateway = new FakeGateway();
        var consumer = NewConsumer(ctx, gateway);

        var act = () => consumer.Consume(ContextFor(SampleSale(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));

        // e-MM için profil gerekli → KeepsRecords bilinmiyor → istisna (retry/error queue).
        await act.Should().ThrowAsync<InvalidOperationException>();
        (await ctx.ProducerReceipts.IgnoreQueryFilters().AnyAsync()).Should().BeFalse();
        gateway.SendCount.Should().Be(0);

        // e-Fatura + HKS istisnadan ÖNCE üretilip kalıcılaştı; retry'da idempotent atlanacaklar.
        (await ctx.Invoices.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await ctx.HksNotifications.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Consume_SameSaleTwice_IsIdempotent_OneEmm()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var producerId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };

        await using var ctx = CreateContext(stub, dbName);
        SeedProfile(ctx, tenantId, producerId, keepsRecords: false);

        var gateway = new FakeGateway();

        var first = NewConsumer(ctx, gateway);
        await first.Consume(ContextFor(SampleSale(tenantId, Guid.NewGuid(), producerId, saleId)));
        await first.Consume(ContextFor(SampleSale(tenantId, Guid.NewGuid(), producerId, saleId)));

        // Her belge türü satış başına tek kez üretilir (docs/04 §5).
        (await ctx.ProducerReceipts.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await ctx.Invoices.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        (await ctx.HksNotifications.IgnoreQueryFilters().CountAsync()).Should().Be(1);
        gateway.SendCount.Should().Be(1);
        gateway.InvoiceCount.Should().Be(1);
        gateway.HksCount.Should().Be(1);
    }
}

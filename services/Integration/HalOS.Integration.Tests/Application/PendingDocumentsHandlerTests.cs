using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Integration.Application.Features.PendingDocuments;
using HalOS.Integration.Domain.Aggregates;
using HalOS.Integration.Infrastructure.Persistence;
using HalOS.Integration.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Integration.Tests.Application;

/// <summary>
/// Bekleyen e-belge özeti (dashboard) entegrasyon testi: GERÇEK InMemory IntegrationDbContext +
/// gerçek repository'ler. Bekleyen = Draft veya Failed; Issued/Cancelled sayılmaz. Tenant filtreli (BK-8).
/// </summary>
public sealed class PendingDocumentsHandlerTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static IntegrationDbContext CreateContext(ITenantContext tenantContext, string dbName) =>
        new(new DbContextOptionsBuilder<IntegrationDbContext>().UseInMemoryDatabase(dbName).Options, tenantContext);

    private static Invoice DraftInvoice(Guid tenantId) =>
        Invoice.CreateCommission(
            tenantId, Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc), 100m, 20m).Value;

    [Fact]
    public async Task PendingDocuments_CountsDraftAndFailed_ExcludesIssued()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            var draft1 = DraftInvoice(tenantId);           // Draft → sayılır
            var draft2 = DraftInvoice(tenantId);           // Draft → sayılır
            var issued = DraftInvoice(tenantId);
            issued.MarkIssued("HAL/2026/000001");          // Issued → sayılmaz
            seed.Invoices.AddRange(draft1, draft2, issued);
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new PendingDocumentsHandler(
            new InvoiceRepository(ctx),
            new ProducerReceiptRepository(ctx),
            new HksNotificationRepository(ctx));

        var result = await handler.Handle(new PendingDocumentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PendingInvoices.Should().Be(2);
        result.Value.PendingProducerReceipts.Should().Be(0);
        result.Value.PendingHksNotifications.Should().Be(0);
        result.Value.Total.Should().Be(2);
    }

    [Fact]
    public async Task PendingDocuments_RespectsTenantFilter()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stubA = new StubTenantContext { TenantId = tenantA };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.Invoices.Add(DraftInvoice(tenantA));
            seed.Invoices.Add(DraftInvoice(tenantB)); // başka tenant → hariç
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stubA, dbName);
        var handler = new PendingDocumentsHandler(
            new InvoiceRepository(ctx),
            new ProducerReceiptRepository(ctx),
            new HksNotificationRepository(ctx));

        var result = await handler.Handle(new PendingDocumentsQuery(), CancellationToken.None);

        result.Value.Total.Should().Be(1);
    }
}

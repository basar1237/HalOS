using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Features.Reports.DashboardSummary;
using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;
using HalOS.Sales.Domain.ValueObjects;
using HalOS.Sales.Infrastructure.Persistence;
using HalOS.Sales.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Sales.Tests.Application;

/// <summary>
/// Dashboard satış-tarafı özeti (docs/02 §5) entegrasyon testi: GERÇEK InMemory DbContext + gerçek
/// repository'ler. "Bugünkü Mal Geliş" (Consignment ReceivedAt gününe göre) ve "Bekleyen Hakediş"
/// (tamamlanmış satışların Settlement.Status ≠ Paid net toplamı). Tenant filtreli (BK-8).
/// </summary>
public sealed class DashboardSummaryHandlerTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static SalesDbContext CreateContext(ITenantContext tenantContext, string dbName) =>
        new(new DbContextOptionsBuilder<SalesDbContext>().UseInMemoryDatabase(dbName).Options, tenantContext);

    private static SaleTransaction CompletedSale(Guid tenantId, decimal gross)
    {
        var sale = SaleTransaction.Create(
            tenantId, Guid.NewGuid(), Guid.NewGuid(), null,
            new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc),
            isWithinMarket: true, operationId: Guid.NewGuid(), createdBy: Guid.NewGuid()).Value;
        sale.AddLine(Guid.NewGuid(), gross, UnitOfMeasure.Kilogram, 1m);
        sale.Complete(RateSet.Create(0.08m, 0.02m, 0.01m, true, 0.20m).Value);
        return sale; // Settlement.Status = Pending (ödenmemiş)
    }

    private static Consignment ConsignmentOn(Guid tenantId, DateTime receivedAt) =>
        Consignment.Receive(
            tenantId, Guid.NewGuid(), receivedAt, "irsaliye", Guid.NewGuid(),
            new[] { new Consignment.ItemInput(Guid.NewGuid(), 10m, UnitOfMeasure.Crate) }).Value;

    private static readonly DateTime Today = new(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Dashboard_CountsTodaysConsignments_AndSumsPendingSettlements()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            // Bekleyen hakediş: net 88 + 176 = 264 (ikisi de Pending).
            seed.SaleTransactions.Add(CompletedSale(tenantId, 100m));
            seed.SaleTransactions.Add(CompletedSale(tenantId, 200m));
            // Bugünkü mal geliş: 2 bugün + 1 dün (sayılmamalı).
            seed.Consignments.Add(ConsignmentOn(tenantId, Today.AddHours(9)));
            seed.Consignments.Add(ConsignmentOn(tenantId, Today.AddHours(14)));
            seed.Consignments.Add(ConsignmentOn(tenantId, Today.AddDays(-1)));
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new DashboardSummaryHandler(
            new ConsignmentRepository(ctx), new SaleTransactionRepository(ctx));

        var result = await handler.Handle(new DashboardSummaryQuery(Today), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TodayConsignmentCount.Should().Be(2);
        result.Value.PendingSettlementTotal.Should().Be(264m);
    }

    [Fact]
    public async Task Dashboard_RespectsTenantFilter()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stubA = new StubTenantContext { TenantId = tenantA };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantA, 100m)); // net 88
            seed.SaleTransactions.Add(CompletedSale(tenantB, 200m)); // başka tenant → hariç
            seed.Consignments.Add(ConsignmentOn(tenantA, Today.AddHours(9)));
            seed.Consignments.Add(ConsignmentOn(tenantB, Today.AddHours(9))); // başka tenant → hariç
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stubA, dbName);
        var handler = new DashboardSummaryHandler(
            new ConsignmentRepository(ctx), new SaleTransactionRepository(ctx));

        var result = await handler.Handle(new DashboardSummaryQuery(Today), CancellationToken.None);

        result.Value.TodayConsignmentCount.Should().Be(1);
        result.Value.PendingSettlementTotal.Should().Be(88m);
    }
}

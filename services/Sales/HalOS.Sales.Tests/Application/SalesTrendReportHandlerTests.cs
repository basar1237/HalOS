using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Contracts;
using HalOS.Sales.Application.Features.Reports.SalesTrendReport;
using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;
using HalOS.Sales.Domain.ValueObjects;
using HalOS.Sales.Infrastructure.Persistence;
using HalOS.Sales.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Sales.Tests.Application;

/// <summary>
/// Satış trend raporu handler'ının GERÇEK InMemory DbContext + GERÇEK
/// <see cref="SaleTransactionRepository"/> ile entegrasyon testleri (docs/06 S2.2, docs/07 §7).
/// Yalnız TAMAMLANMIŞ satışlar zaman kovalarına gruplanır; tenant global filter otomatik (BK-8);
/// tutarlar decimal (BK-2). Yeni tablo/migration yok — mevcut veri okunur.
///
/// Oran kümesi (0.08/0.02/0.01/hal içi→rüsum 0.01/KDV 0.20):
/// - brüt 100 → komisyon 8, net 88. brüt 200 → komisyon 16, net 176.
/// </summary>
public sealed class SalesTrendReportHandlerTests
{
    private sealed class StubTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static SalesDbContext CreateContext(ITenantContext tenantContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SalesDbContext(options, tenantContext);
    }

    private static SaleTransaction CompletedSale(Guid tenantId, DateTime soldAt, decimal gross)
    {
        var sale = SaleTransaction.Create(
            tenantId, Guid.NewGuid(), Guid.NewGuid(), null,
            soldAt, isWithinMarket: true, operationId: Guid.NewGuid(), createdBy: Guid.NewGuid()).Value;
        sale.AddLine(Guid.NewGuid(), gross, UnitOfMeasure.Kilogram, 1m);
        sale.Complete(RateSet.Create(0.08m, 0.02m, 0.01m, true, 0.20m).Value);
        return sale;
    }

    // Aynı gün (06.07) iki satış, ertesi gün (07.07) bir satış — gün kovalaması sınaması.
    private static readonly DateTime Day1Morning = new(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day1Evening = new(2026, 7, 6, 18, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day2 = new(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Trend_Daily_TwoDaysThreeSales_BucketsPerDayWithCorrectTotals()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1Morning, 100m)); // gün 1
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1Evening, 200m)); // gün 1
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day2, 100m));        // gün 2
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new SalesTrendReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new SalesTrendReportQuery(Day1Morning.Date, Day2.Date, TrendGranularity.Day),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var buckets = result.Value.Buckets;
        result.Value.Granularity.Should().Be(TrendGranularity.Day);
        buckets.Should().HaveCount(2);

        // Gün 1 kovası: 2 satış, brüt 300, komisyon 24, net 264.
        buckets[0].PeriodStart.Should().Be(new DateTime(2026, 7, 6));
        buckets[0].Count.Should().Be(2);
        buckets[0].Gross.Should().Be(300m);
        buckets[0].Commission.Should().Be(24m);
        buckets[0].Net.Should().Be(264m);

        // Gün 2 kovası: 1 satış, brüt 100, komisyon 8, net 88.
        buckets[1].PeriodStart.Should().Be(new DateTime(2026, 7, 7));
        buckets[1].Count.Should().Be(1);
        buckets[1].Gross.Should().Be(100m);
        buckets[1].Commission.Should().Be(8m);
        buckets[1].Net.Should().Be(88m);
    }

    [Fact]
    public async Task Trend_Weekly_GroupsSalesIntoIsoWeekStartMonday()
    {
        // 06.07.2026 Pazartesi, 07.07 Salı → aynı ISO hafta (başlangıç 06.07 Pzt).
        // 13.07.2026 Pazartesi → sonraki hafta.
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();
        var nextWeek = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1Morning, 100m)); // hafta 1 (Pzt)
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day2, 200m));        // hafta 1 (Salı)
            seed.SaleTransactions.Add(CompletedSale(tenantId, nextWeek, 100m));    // hafta 2 (Pzt)
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new SalesTrendReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new SalesTrendReportQuery(Day1Morning.Date, nextWeek.Date, TrendGranularity.Week),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var buckets = result.Value.Buckets;
        buckets.Should().HaveCount(2);

        buckets[0].PeriodStart.Should().Be(new DateTime(2026, 7, 6)); // hafta başı Pzt
        buckets[0].Count.Should().Be(2);
        buckets[0].Gross.Should().Be(300m);

        buckets[1].PeriodStart.Should().Be(new DateTime(2026, 7, 13)); // sonraki hafta başı Pzt
        buckets[1].Count.Should().Be(1);
        buckets[1].Gross.Should().Be(100m);
    }

    [Fact]
    public async Task Trend_Monthly_GroupsSalesIntoMonthStart()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();
        var august = new DateTime(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc);

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1Morning, 100m)); // Temmuz
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day2, 200m));        // Temmuz
            seed.SaleTransactions.Add(CompletedSale(tenantId, august, 100m));      // Ağustos
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new SalesTrendReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new SalesTrendReportQuery(Day1Morning.Date, august.Date, TrendGranularity.Month),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var buckets = result.Value.Buckets;
        buckets.Should().HaveCount(2);

        buckets[0].PeriodStart.Should().Be(new DateTime(2026, 7, 1)); // Temmuz başı
        buckets[0].Count.Should().Be(2);
        buckets[0].Gross.Should().Be(300m);

        buckets[1].PeriodStart.Should().Be(new DateTime(2026, 8, 1)); // Ağustos başı
        buckets[1].Count.Should().Be(1);
        buckets[1].Gross.Should().Be(100m);
    }

    [Fact]
    public async Task Trend_ExcludesDraftAndOutOfRangeAndOtherTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stubA = new StubTenantContext { TenantId = tenantA };
        var dbName = Guid.NewGuid().ToString();

        // Taslak (tamamlanmamış) satış: rapora girmemeli.
        var draft = SaleTransaction.Create(
            tenantA, Guid.NewGuid(), Guid.NewGuid(), null,
            Day1Morning, true, Guid.NewGuid(), Guid.NewGuid()).Value;
        draft.AddLine(Guid.NewGuid(), 999m, UnitOfMeasure.Kilogram, 1m);

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantA, Day1Morning, 100m)); // aralık içi, tenant A
            seed.SaleTransactions.Add(draft);                                     // taslak → hariç
            seed.SaleTransactions.Add(CompletedSale(tenantB, Day1Morning, 500m)); // başka tenant → hariç
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stubA, dbName);
        var handler = new SalesTrendReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new SalesTrendReportQuery(Day1Morning.Date, Day1Morning.Date, TrendGranularity.Day),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Buckets.Should().HaveCount(1);
        result.Value.Buckets[0].Count.Should().Be(1);   // yalnız tamamlanmış tenant A satışı
        result.Value.Buckets[0].Gross.Should().Be(100m);
    }

    [Fact]
    public async Task Trend_EmptyRange_ReturnsEmptyBuckets()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using var ctx = CreateContext(stub, dbName);
        var handler = new SalesTrendReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new SalesTrendReportQuery(Day1Morning.Date, Day2.Date, TrendGranularity.Day),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Buckets.Should().BeEmpty();
    }

    [Fact]
    public void TrendValidator_RejectsFromAfterTo()
    {
        var validator = new SalesTrendReportValidator();
        var result = validator.Validate(
            new SalesTrendReportQuery(Day2, Day1Morning, TrendGranularity.Day));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TrendValidator_AllowsFromEqualsTo()
    {
        var validator = new SalesTrendReportValidator();
        var result = validator.Validate(
            new SalesTrendReportQuery(Day1Morning, Day1Morning, TrendGranularity.Day));
        result.IsValid.Should().BeTrue();
    }
}

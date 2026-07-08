using FluentAssertions;
using HalOS.BuildingBlocks.Application;
using HalOS.Sales.Application.Features.Reports.CommissionIncomeReport;
using HalOS.Sales.Application.Features.Reports.DailySummaryReport;
using HalOS.Sales.Application.Features.Reports.SalesSummaryReport;
using HalOS.Sales.Domain.Aggregates;
using HalOS.Sales.Domain.Enums;
using HalOS.Sales.Domain.ValueObjects;
using HalOS.Sales.Infrastructure.Persistence;
using HalOS.Sales.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HalOS.Sales.Tests.Application;

/// <summary>
/// Satış raporu handler'larının GERÇEK InMemory DbContext + GERÇEK <see cref="SaleTransactionRepository"/>
/// ile entegrasyon testleri (docs/03 M10, docs/07 §7). Yalnız TAMAMLANMIŞ satışlar üzerinden
/// agregasyon; tenant global filter otomatik (BK-8); tutarlar decimal (BK-2). Yeni tablo/migration
/// yok — mevcut veri okunur.
///
/// Oran kümesi (RateSet 0.08/0.02/0.01/hal içi→rüsum 0.01/KDV 0.20):
/// - brüt 100 → komisyon 8, KDV 1,60, kesinti(KDV hariç) 12, net 88.
/// - brüt 200 → komisyon 16, KDV 3,20, kesinti(KDV hariç) 24, net 176.
/// </summary>
public sealed class SalesReportsHandlerTests
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
        // Satır: miktar = brüt, birim fiyat = 1 → satır tutarı = brüt.
        sale.AddLine(Guid.NewGuid(), gross, UnitOfMeasure.Kilogram, 1m);
        sale.Complete(RateSet.Create(0.08m, 0.02m, 0.01m, true, 0.20m).Value);
        return sale;
    }

    private static SaleTransaction DraftSale(Guid tenantId, DateTime soldAt, decimal gross)
    {
        var sale = SaleTransaction.Create(
            tenantId, Guid.NewGuid(), Guid.NewGuid(), null,
            soldAt, isWithinMarket: true, operationId: Guid.NewGuid(), createdBy: Guid.NewGuid()).Value;
        sale.AddLine(Guid.NewGuid(), gross, UnitOfMeasure.Kilogram, 1m);
        return sale; // tamamlanmadı → raporlara girmemeli.
    }

    private static readonly DateTime Day1 = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day2 = new(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SalesSummary_TwoCompletedSales_AggregatesCountGrossCommissionDeductionsNet()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1, 100m));
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1, 200m));
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new SalesSummaryReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new SalesSummaryReportQuery(Day1.AddDays(-1), Day1.AddDays(1)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Count.Should().Be(2);
        dto.TotalGross.Should().Be(300m);
        dto.TotalCommission.Should().Be(24m);      // 8 + 16
        dto.TotalDeductions.Should().Be(36m);       // (8+2+1+1) + (16+4+2+2), KDV hariç
        dto.TotalNet.Should().Be(264m);             // 88 + 176
    }

    [Fact]
    public async Task SalesSummary_ExcludesSalesOutsideRange()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1, 100m)); // aralık içi
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day2, 200m)); // aralık DIŞI
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new SalesSummaryReportHandler(new SaleTransactionRepository(ctx));

        // Yalnız Day1'i kapsayan aralık: ToUtc gün bazında DAHİL, çağıran gün-sonu inşa etmez.
        var result = await handler.Handle(
            new SalesSummaryReportQuery(Day1.Date, Day1.Date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(1);
        result.Value.TotalGross.Should().Be(100m);
        result.Value.TotalNet.Should().Be(88m);
    }

    [Fact]
    public async Task SalesSummary_IncludesSalesOnEndDay_WhenToIsDayStart()
    {
        // Regresyon: to=07.07 (gün başı) verildiğinde 07.07 10:00 satışı DÜŞMEMELİ.
        // Eski `SoldAt <= toUtc` semantiğinde bu satış sessizce hariç kalırdı.
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1, 100m)); // 06.07 10:00
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day2, 200m)); // 07.07 10:00 — bitiş günü
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new SalesSummaryReportHandler(new SaleTransactionRepository(ctx));

        // "06.07'den 07.07'ye kadar" — to=07.07 gün başı; 07.07'nin tüm saatleri dahil olmalı.
        var result = await handler.Handle(
            new SalesSummaryReportQuery(Day1.Date, Day2.Date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(2);
        result.Value.TotalGross.Should().Be(300m);
        result.Value.TotalNet.Should().Be(264m);
    }

    [Fact]
    public async Task CommissionIncome_IncludesSalesOnEndDay_WhenToIsDayStart()
    {
        // Regresyon: komisyon geliri raporu da bitiş gününün tüm saatlerini kapsamalı.
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day2, 100m)); // 07.07 10:00 — bitiş günü
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new CommissionIncomeReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new CommissionIncomeReportQuery(Day1.Date, Day2.Date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCommission.Should().Be(8m);   // 07.07 satışı sayıldı
        result.Value.TotalVat.Should().Be(1.60m);
    }

    [Fact]
    public async Task SalesSummary_ExcludesDraftSales()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1, 100m));
            seed.SaleTransactions.Add(DraftSale(tenantId, Day1, 999m)); // taslak → sayılmamalı
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new SalesSummaryReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new SalesSummaryReportQuery(Day1.AddDays(-1), Day1.AddDays(1)), CancellationToken.None);

        result.Value.Count.Should().Be(1);
        result.Value.TotalGross.Should().Be(100m);
    }

    [Fact]
    public async Task CommissionIncome_SumsCommissionPlusVat()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1, 100m));
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1, 200m));
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new CommissionIncomeReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new CommissionIncomeReportQuery(Day1.AddDays(-1), Day1.AddDays(1)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCommission.Should().Be(24m);    // 8 + 16
        result.Value.TotalVat.Should().Be(4.80m);         // 1,60 + 3,20
        result.Value.TotalIncome.Should().Be(28.80m);     // komisyon + KDV
        result.Value.Daily.Should().BeEmpty();            // kırılım istenmedi
    }

    [Fact]
    public async Task CommissionIncome_DailyBreakdown_GroupsBySoldAtDate()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1, 100m)); // gün 1: komisyon 8, KDV 1,60
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day2, 200m)); // gün 2: komisyon 16, KDV 3,20
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new CommissionIncomeReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new CommissionIncomeReportQuery(Day1.Date, Day2.Date.AddDays(1), IncludeDailyBreakdown: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Daily.Should().HaveCount(2);
        result.Value.Daily[0].Day.Should().Be(Day1.Date);
        result.Value.Daily[0].Commission.Should().Be(8m);
        result.Value.Daily[0].Vat.Should().Be(1.60m);
        result.Value.Daily[0].Income.Should().Be(9.60m);
        result.Value.Daily[1].Day.Should().Be(Day2.Date);
        result.Value.Daily[1].Commission.Should().Be(16m);
        result.Value.TotalIncome.Should().Be(28.80m);
    }

    [Fact]
    public async Task DailySummary_OnlyThatDaysCompletedSales()
    {
        var tenantId = Guid.NewGuid();
        var stub = new StubTenantContext { TenantId = tenantId };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stub, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1, 100m)); // hedef gün
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day1, 200m)); // hedef gün
            seed.SaleTransactions.Add(CompletedSale(tenantId, Day2, 500m)); // başka gün → hariç
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stub, dbName);
        var handler = new DailySummaryReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(new DailySummaryReportQuery(Day1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Day.Should().Be(Day1.Date);
        dto.Count.Should().Be(2);
        dto.Gross.Should().Be(300m);
        dto.Commission.Should().Be(24m);   // 8 + 16
        dto.Net.Should().Be(264m);         // 88 + 176
    }

    [Fact]
    public async Task Reports_RespectTenantFilter_OtherTenantExcluded()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var stubA = new StubTenantContext { TenantId = tenantA };
        var dbName = Guid.NewGuid().ToString();

        await using (var seed = CreateContext(stubA, dbName))
        {
            seed.SaleTransactions.Add(CompletedSale(tenantA, Day1, 100m));
            seed.SaleTransactions.Add(CompletedSale(tenantB, Day1, 200m)); // başka tenant → görünmemeli
            await seed.SaveChangesAsync();
        }

        await using var ctx = CreateContext(stubA, dbName);
        var handler = new SalesSummaryReportHandler(new SaleTransactionRepository(ctx));

        var result = await handler.Handle(
            new SalesSummaryReportQuery(Day1.AddDays(-1), Day1.AddDays(1)), CancellationToken.None);

        result.Value.Count.Should().Be(1);
        result.Value.TotalGross.Should().Be(100m);
    }

    [Fact]
    public void SalesSummaryValidator_RejectsFromAfterTo()
    {
        var validator = new SalesSummaryReportValidator();
        var result = validator.Validate(new SalesSummaryReportQuery(Day2, Day1));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CommissionIncomeValidator_AllowsFromEqualsTo()
    {
        var validator = new CommissionIncomeReportValidator();
        var result = validator.Validate(new CommissionIncomeReportQuery(Day1, Day1));
        result.IsValid.Should().BeTrue();
    }
}
